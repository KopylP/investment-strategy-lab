from __future__ import annotations

from dataclasses import dataclass, field, replace
from datetime import date

import numpy as np
import pandas as pd

from finance_decisions.helpers import (
    last_valid_date,
    latest_moving_average,
    latest_rolling_high,
    latest_value,
    money,
    safe_division,
)
from finance_decisions.models import Position, aggregate_positions
from .config import RiskSettings

STATUS_LABELS = {
    "green": "у нормі",
    "yellow": "увага",
    "red": "продати",
    "unknown": "немає даних",
    "cash": "кеш",
}

MAX_STALE_DAYS = 5


@dataclass(frozen=True)
class PositionRisk:
    ticker: str
    status: str
    current_price: float
    pnl_pct: float
    pnl_abs: float
    drawdown_from_high: float
    above_ma: bool
    currency: str = "USD"
    actions: list[str] = field(default_factory=list)
    alerts: list[str] = field(default_factory=list)
    name: str = ""
    shares: float = 0.0
    value: float = 0.0
    entry_price: float = 0.0
    ma_value: float | None = None
    trail_high: float | None = None
    stop_price: float | None = None
    ma_exit_price: float | None = None
    trail_exit_price: float | None = None
    exit_price: float | None = None
    exit_rule: str = ""
    exit_distance: float | None = None
    stop_loss_threshold: float | None = None
    drawdown_exit_threshold: float | None = None
    drawdown_warn_threshold: float | None = None
    sell: bool = False


@dataclass(frozen=True)
class RiskReport:
    as_of: date
    spy_above_ma: bool
    vix: float | None
    market_alerts: list[str]
    position_risks: list[PositionRisk]
    portfolio_pnl: float | None
    portfolio_value: float
    portfolio_cost: float
    actions: list[str]
    currency: str = "USD"
    portfolio_alerts: list[str] = field(default_factory=list)
    spy_price: float | None = None
    spy_ma: float | None = None
    portfolio_drawdown: float | None = None
    portfolio_peak_date: date | None = None
    cash: float = 0.0
    settings: RiskSettings | None = None
    cash_etf: str | None = None

    @property
    def alerts(self) -> list[str]:
        items = list(self.market_alerts)
        for risk in self.position_risks:
            items.extend(risk.alerts)
        items.extend(self.portfolio_alerts)
        return items

    @property
    def status(self) -> str:
        """'act' when something must be sold, 'watch' for warnings, 'ok' otherwise."""
        if any(r.status == "red" for r in self.position_risks) or self.portfolio_exit_hit:
            return "act"
        if self.alerts:
            return "watch"
        return "ok"

    @property
    def portfolio_exit_hit(self) -> bool:
        limit = self.settings.portfolio_exit if self.settings else RiskSettings.portfolio_exit
        return self.portfolio_drawdown is not None and self.portfolio_drawdown <= limit


def analyze_portfolio_risk(
    prices: pd.DataFrame,
    positions: list[Position],
    settings: RiskSettings | None = None,
    *,
    cash: float = 0.0,
    cash_etf: str | None = None,
    names: dict[str, str] | None = None,
) -> RiskReport:
    """Check every position against its exit rules.

    *positions* may contain several lots of the same ticker; they are combined for
    the per-position checks and kept separate for the portfolio drawdown history.
    """
    settings = settings or RiskSettings()
    names = names or {}
    prices = prices.dropna(how="all")
    if prices.empty:
        raise ValueError("No price data available.")
    as_of = prices.index[-1]

    spy_price, spy_ma, spy_above_ma = _spy_trend(prices, as_of, settings.ma_period)
    vix = _latest_fresh(prices, "^VIX", as_of)

    market_alerts: list[str] = []
    actions: list[str] = []

    if spy_price is None or spy_ma is None:
        market_alerts.append("Немає свіжих даних SPY: тренд ринку перевірити неможливо.")
    elif not spy_above_ma:
        market_alerts.append(
            f"SPY нижче 200-денної середньої ({spy_price:.2f} проти {spy_ma:.2f}): ринок слабкий. "
            "Наступний місячний план інвестує лише частину внеску; примусово нічого не продаємо."
        )

    high_stress = False
    if vix is None:
        market_alerts.append("Немає свіжих даних VIX: перевірте рівень стресу ринку вручну.")
    elif vix > settings.vix_exit:
        high_stress = True
        market_alerts.append(f"VIX {vix:.1f} вище {settings.vix_exit:.0f}: високий стрес. Позиції з попередженнями продаємо вже зараз.")
    elif vix > settings.vix_warn:
        market_alerts.append(f"VIX {vix:.1f} вище {settings.vix_warn:.0f}: волатильність підвищена, будьте уважні.")

    aggregated = aggregate_positions(positions)
    currencies = {position.currency for position in aggregated}
    report_currency = currencies.pop() if len(currencies) == 1 else ("USD" if not currencies else "MIXED")
    parking = cash_etf or "кеш"

    risks: list[PositionRisk] = []
    total_value = cash
    total_cost = cash
    for position in aggregated:
        risk = _position_risk(prices, position, settings, as_of, names.get(position.ticker, position.ticker), position.ticker == cash_etf)
        if risk.status != "unknown":
            total_value += risk.value
            total_cost += position.entry_price * position.shares
        if risk.status == "red" or (high_stress and risk.status == "yellow"):
            reason = risk.alerts[0] if risk.alerts else ""
            action = f"Продати {position.ticker} повністю (≈ {money(risk.value, position.currency)}), гроші перевести в {parking}. Причина: {reason}"
            risk = _replace_actions(risk, [action], sell=True)
        elif risk.status == "yellow" and risk.exit_price is not None:
            risk = _replace_actions(
                risk,
                [f"Нічого не продавати. Стежити за {position.ticker}: вихід, якщо ціна закриття опуститься нижче {money(risk.exit_price, position.currency)}."],
            )
        actions.extend(risk.actions)
        risks.append(risk)

    portfolio_pnl = total_value / total_cost - 1 if total_cost > 0 else None
    drawdown, peak_date = portfolio_drawdown(prices, positions)

    portfolio_alerts: list[str] = []
    if drawdown is not None:
        if drawdown <= settings.portfolio_exit:
            portfolio_alerts.append(f"Портфель на {-drawdown:.1%} нижче свого піку: досягнуто ліміт {settings.portfolio_exit:.0%}.")
            actions.append(f"Продати всі позиції й перевести гроші в {parking}. Повертатися в ринок за наступним місячним планом.")
        elif drawdown <= settings.portfolio_warn:
            portfolio_alerts.append(f"Портфель на {-drawdown:.1%} нижче свого піку (попередження при {settings.portfolio_warn:.0%}, вихід при {settings.portfolio_exit:.0%}).")

    return RiskReport(
        as_of=as_of.date(),
        spy_above_ma=spy_above_ma,
        vix=vix,
        market_alerts=market_alerts,
        position_risks=risks,
        portfolio_pnl=portfolio_pnl,
        portfolio_value=total_value,
        portfolio_cost=total_cost,
        actions=list(dict.fromkeys(actions)),
        currency=report_currency,
        portfolio_alerts=portfolio_alerts,
        spy_price=spy_price,
        spy_ma=spy_ma,
        portfolio_drawdown=drawdown,
        portfolio_peak_date=peak_date,
        cash=cash,
        settings=settings,
        cash_etf=cash_etf,
    )


def portfolio_drawdown(prices: pd.DataFrame, lots: list[Position]) -> tuple[float | None, date | None]:
    """Drawdown of the time-weighted portfolio return index from its peak.

    Each lot counts from its entry date, so new contributions neither create nor
    hide drawdowns.
    """
    tickers = sorted({lot.ticker for lot in lots if lot.ticker in prices.columns})
    if not tickers:
        return None, None
    frame = prices[tickers].ffill()
    shares = pd.DataFrame(0.0, index=frame.index, columns=tickers)
    for lot in lots:
        if lot.ticker not in shares.columns:
            continue
        start = _entry_timestamp(lot.entry_date, frame.index)
        mask = frame.index >= start if start is not None else np.ones(len(frame.index), dtype=bool)
        shares.loc[mask, lot.ticker] += lot.shares

    held = shares.shift(1).fillna(0.0)
    base = (held * frame.shift(1)).sum(axis=1)
    pnl = (held * frame.diff()).sum(axis=1)
    returns = (pnl / base).where(base > 0)
    returns = returns[returns.notna()]
    if returns.empty:
        return None, None
    index = (1 + returns).cumprod()
    peak = index.cummax()
    return float(index.iloc[-1] / peak.iloc[-1] - 1), index.idxmax().date()


def _position_risk(
    prices: pd.DataFrame,
    position: Position,
    settings: RiskSettings,
    as_of: pd.Timestamp,
    name: str,
    is_cash: bool,
) -> PositionRisk:
    def unknown(message: str) -> PositionRisk:
        return PositionRisk(
            ticker=position.ticker,
            status="unknown",
            current_price=0.0,
            pnl_pct=0.0,
            pnl_abs=0.0,
            drawdown_from_high=0.0,
            above_ma=False,
            currency=position.currency,
            alerts=[f"{position.ticker}: {message}"],
            actions=[f"Перевірити {position.ticker} вручну: {message}"],
            name=name,
            shares=position.shares,
            entry_price=position.entry_price,
        )

    if position.ticker not in prices.columns:
        return unknown("ринкові дані недоступні.")
    series = prices[position.ticker].dropna()
    current_price = latest_value(series)
    if current_price is None:
        return unknown("ринкові дані недоступні.")
    last = last_valid_date(series)
    if (as_of - last).days > MAX_STALE_DAYS:
        return unknown(f"остання ціна від {last:%d.%m.%Y}, дані застарілі.")

    value = current_price * position.shares
    pnl_pct = safe_division(current_price, position.entry_price) - 1
    pnl_abs = value - position.entry_price * position.shares
    common = dict(
        ticker=position.ticker,
        current_price=current_price,
        pnl_pct=pnl_pct,
        pnl_abs=pnl_abs,
        currency=position.currency,
        name=name,
        shares=position.shares,
        value=value,
        entry_price=position.entry_price,
    )
    if is_cash:
        return PositionRisk(status="cash", drawdown_from_high=0.0, above_ma=True, **common)

    moving_average = latest_moving_average(series, settings.ma_period)
    trail_high = _trailing_high(series, position)
    drawdown = (current_price - trail_high) / trail_high if trail_high and trail_high > 0 else 0.0
    above_ma = bool(moving_average is not None and current_price > moving_average)

    # Wider stops for volatile ETFs, tighter for calm ones.
    daily_returns = series.pct_change().dropna().tail(60)
    asset_vol = float(daily_returns.std() * np.sqrt(252)) if len(daily_returns) >= 10 else settings.vol_reference
    scale = asset_vol / settings.vol_reference
    stop_threshold = min(-0.04, max(-0.25, settings.stop_loss_pct * scale))
    exit_threshold = min(-0.08, max(-0.35, settings.drawdown_exit * scale))
    warn_threshold = min(-0.05, max(-0.20, settings.drawdown_warn * scale))

    stop_price = position.entry_price * (1 + stop_threshold)
    ma_exit_price = moving_average * (1 - settings.ma_exit_band) if moving_average is not None else None
    trail_exit_price = trail_high * (1 + exit_threshold) if trail_high else None
    levels = {
        "стоп-лосс від ціни входу": stop_price,
        f"MA200 мінус {settings.ma_exit_band:.0%}": ma_exit_price,
        "просадка від максимуму": trail_exit_price,
    }
    exit_rule, exit_price = max(((rule, level) for rule, level in levels.items() if level is not None), key=lambda item: item[1])

    alerts: list[str] = []
    status = "green"
    if current_price <= stop_price:
        status = "red"
        alerts.append(f"{position.ticker}: спрацював стоп-лосс ({pnl_pct:+.1%} при ліміті {stop_threshold:.1%}).")
    if ma_exit_price is not None and current_price < ma_exit_price:
        status = "red"
        alerts.append(f"{position.ticker}: ціна більш ніж на {settings.ma_exit_band:.0%} нижче MA200 ({money(moving_average, position.currency)}).")
    if drawdown <= exit_threshold:
        status = "red"
        alerts.append(f"{position.ticker}: просадка від максимуму {drawdown:.1%} при ліміті {exit_threshold:.1%}.")

    if status != "red":
        if moving_average is None:
            status = "yellow"
            alerts.append(f"{position.ticker}: замало історії для MA200.")
        elif not above_ma:
            status = "yellow"
            alerts.append(f"{position.ticker}: ціна нижче MA200, але в межах допуску {settings.ma_exit_band:.0%}.")
        if drawdown <= warn_threshold:
            status = "yellow"
            alerts.append(f"{position.ticker}: просадка {drawdown:.1%} (попередження при {warn_threshold:.1%}).")
        if pnl_pct < settings.small_loss_warn:
            status = "yellow"
            alerts.append(f"{position.ticker}: збиток {pnl_pct:+.1%} від ціни входу.")

    return PositionRisk(
        status=status,
        drawdown_from_high=drawdown,
        above_ma=above_ma,
        alerts=alerts,
        ma_value=moving_average,
        trail_high=trail_high,
        stop_price=stop_price,
        ma_exit_price=ma_exit_price,
        trail_exit_price=trail_exit_price,
        exit_price=exit_price,
        exit_rule=exit_rule,
        exit_distance=exit_price / current_price - 1,
        stop_loss_threshold=stop_threshold,
        drawdown_exit_threshold=exit_threshold,
        drawdown_warn_threshold=warn_threshold,
        **common,
    )


def _replace_actions(risk: PositionRisk, actions: list[str], sell: bool = False) -> PositionRisk:
    return replace(risk, actions=actions, sell=sell)


def _trailing_high(series: pd.Series, position: Position) -> float | None:
    high = latest_rolling_high(series, 252)
    start = _entry_timestamp(position.entry_date, series.index)
    if start is None:
        return high
    since_entry = series.loc[series.index >= start]
    if since_entry.empty:
        return position.entry_price
    return max(float(since_entry.max()), position.entry_price)


def _entry_timestamp(entry_date: str | None, index: pd.Index) -> pd.Timestamp | None:
    if not entry_date:
        return None
    try:
        timestamp = pd.Timestamp(entry_date)
    except (TypeError, ValueError):
        return None
    tz = getattr(index, "tz", None)
    if tz is not None and timestamp.tz is None:
        timestamp = timestamp.tz_localize(tz)
    elif tz is None and timestamp.tz is not None:
        timestamp = timestamp.tz_convert(None)
    return timestamp


def _spy_trend(prices: pd.DataFrame, as_of: pd.Timestamp, ma_period: int) -> tuple[float | None, float | None, bool]:
    if "SPY" not in prices.columns:
        return None, None, False
    last = last_valid_date(prices["SPY"])
    if last is None or (as_of - last).days > MAX_STALE_DAYS:
        return None, None, False
    price = latest_value(prices["SPY"])
    ma = latest_moving_average(prices["SPY"], ma_period)
    return price, ma, bool(price is not None and ma is not None and price > ma)


def _latest_fresh(prices: pd.DataFrame, ticker: str, as_of: pd.Timestamp) -> float | None:
    """Last valid close (not the last row, which may be a non-US trading day)."""
    if ticker not in prices.columns:
        return None
    last = last_valid_date(prices[ticker])
    if last is None or (as_of - last).days > MAX_STALE_DAYS:
        return None
    return latest_value(prices[ticker])


def format_risk_report(report: RiskReport) -> str:
    verdict = {
        "ok": "Дій не потрібно. Усі позиції в межах правил.",
        "watch": "Продавати нічого не треба, але є попередження.",
        "act": "Потрібна дія: виконайте кроки нижче.",
    }[report.status]
    lines = [f"Тижнева перевірка ризиків | дані на {report.as_of:%d.%m.%Y}", "", verdict]

    if report.actions:
        lines.extend(["", "Що зробити:"])
        lines.extend(f"{index}. {action}" for index, action in enumerate(report.actions, 1))

    lines.extend(["", "Позиції:"])
    if not report.position_risks:
        lines.append("- Позицій немає.")
    for risk in report.position_risks:
        label = STATUS_LABELS.get(risk.status, risk.status)
        if risk.status in ("unknown",):
            lines.append(f"- {risk.ticker}: {label}")
            continue
        line = f"- {risk.ticker} [{label}]: {money(risk.current_price, risk.currency)}, P&L {risk.pnl_pct:+.1%}"
        if risk.exit_price is not None:
            line += f", вихід нижче {money(risk.exit_price, risk.currency)} ({risk.exit_distance:+.1%})"
        lines.append(line)

    if report.portfolio_pnl is not None:
        lines.extend(
            [
                "",
                f"Портфель: {money(report.portfolio_value, report.currency, decimals=0)} "
                f"(вкладено {money(report.portfolio_cost, report.currency, decimals=0)}, P&L {report.portfolio_pnl:+.1%})",
            ]
        )
        if report.portfolio_drawdown is not None:
            lines.append(f"Від піку: {report.portfolio_drawdown:+.1%}")

    trend = "немає даних" if report.spy_price is None else ("вище MA200" if report.spy_above_ma else "нижче MA200")
    vix = f"{report.vix:.1f}" if report.vix is not None else "немає даних"
    lines.extend(["", f"Ринок: SPY {trend}, VIX {vix}"])

    if report.alerts:
        lines.extend(["", f"Сигнали ({len(report.alerts)}):"])
        lines.extend(f"- {alert}" for alert in report.alerts)
    return "\n".join(lines)
