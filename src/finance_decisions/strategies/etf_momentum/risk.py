from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date

import pandas as pd

from finance_decisions.helpers import (
    is_na,
    latest_moving_average,
    latest_rolling_high,
    latest_value,
    money,
    safe_division,
)
from finance_decisions.models import Position
from .config import RiskSettings


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

    @property
    def alerts(self) -> list[str]:
        items = list(self.market_alerts)
        for risk in self.position_risks:
            items.extend(risk.alerts)
        items.extend(self.portfolio_alerts)
        return items


def analyze_portfolio_risk(
    prices: pd.DataFrame,
    positions: list[Position],
    settings: RiskSettings | None = None,
) -> RiskReport:
    settings = settings or RiskSettings()
    if prices.empty:
        raise ValueError("No price data available.")
    today = prices.iloc[-1]
    spy_latest = latest_value(prices["SPY"]) if "SPY" in prices.columns else None
    spy_ma = latest_moving_average(prices["SPY"], settings.ma_period) if "SPY" in prices.columns else None
    spy_above_ma = bool(spy_latest > spy_ma) if spy_latest is not None and spy_ma is not None else False
    vix_value = today.get("^VIX")
    vix = float(vix_value) if not is_na(vix_value) else None

    market_alerts: list[str] = []
    actions: list[str] = []

    if not spy_above_ma:
        market_alerts.append("SPY нижче MA200: широкий тренд ринку слабкий.")
        actions.append("Зменшіть ризик портфеля або збільшіть частку кешу.")

    if vix is not None:
        if vix > settings.vix_exit:
            market_alerts.append(f"VIX становить {vix:.1f}: високий рівень ринкового стресу.")
            actions.append("Зменшіть частку інвестицій під час підвищеної волатильності.")
        elif vix > settings.vix_warn:
            market_alerts.append(f"VIX становить {vix:.1f}: волатильність підвищена.")

    total_value = 0.0
    total_cost = 0.0
    currencies = {position.currency for position in positions}
    report_currency = currencies.pop() if len(currencies) == 1 else ("USD" if not currencies else "MIXED")
    risks: list[PositionRisk] = []

    for position in positions:
        if position.ticker not in prices.columns:
            risks.append(
                PositionRisk(
                    ticker=position.ticker,
                    status="unknown",
                    current_price=0.0,
                    pnl_pct=0.0,
                    pnl_abs=0.0,
                    drawdown_from_high=0.0,
                    above_ma=False,
                    currency=position.currency,
                    alerts=[f"{position.ticker}: ринкові дані ціни недоступні."],
                )
            )
            continue

        price_series = prices[position.ticker]
        current_price = latest_value(price_series)
        moving_average = latest_moving_average(price_series, settings.ma_period)

        # 1. Trailing high since entry date/price
        ticker_high = latest_rolling_high(price_series, 252)
        if position.entry_date:
            try:
                sorted_prices = price_series.sort_index()
                entry_ts = pd.to_datetime(position.entry_date)
                if isinstance(sorted_prices.index, pd.DatetimeIndex):
                    # Align timezones
                    if sorted_prices.index.tz is not None and entry_ts.tz is None:
                        entry_ts = entry_ts.tz_localize(sorted_prices.index.tz)
                    elif sorted_prices.index.tz is None and entry_ts.tz is not None:
                        entry_ts = entry_ts.tz_convert(None)

                since_entry = sorted_prices.loc[entry_ts:]
                if not since_entry.empty:
                    highest_close = since_entry.max()
                    ticker_high = max(float(highest_close), position.entry_price) if pd.notna(highest_close) else position.entry_price
            except Exception:
                pass

        if current_price is None:
            risks.append(
                PositionRisk(
                    ticker=position.ticker,
                    status="unknown",
                    current_price=0.0,
                    pnl_pct=0.0,
                    pnl_abs=0.0,
                    drawdown_from_high=0.0,
                    above_ma=False,
                    currency=position.currency,
                    alerts=[f"{position.ticker}: ринкові дані ціни недоступні."],
                )
            )
            continue

        cost_basis = position.entry_price * position.shares
        current_value = current_price * position.shares
        pnl_pct = safe_division(current_price, position.entry_price) - 1
        pnl_abs = current_value - cost_basis
        drawdown = (current_price - ticker_high) / ticker_high if ticker_high is not None and ticker_high > 0 else 0.0
        above_ma = bool(current_price > moving_average) if moving_average is not None else False

        total_value += current_value
        total_cost += cost_basis

        # 2. Dynamic Volatility-adjusted stop & drawdown exits
        daily_returns = price_series.pct_change().dropna().tail(60)
        if len(daily_returns) >= 10:
            import numpy as np
            asset_vol = float(daily_returns.std() * np.sqrt(252))
        else:
            asset_vol = 0.15

        base_stop = settings.stop_loss_pct
        vol_adjusted_stop = base_stop * (asset_vol / 0.15)
        stop_loss_threshold = min(-0.04, max(-0.25, vol_adjusted_stop))

        base_drawdown_exit = settings.drawdown_exit
        vol_adjusted_drawdown_exit = base_drawdown_exit * (asset_vol / 0.15)
        drawdown_exit_threshold = min(-0.08, max(-0.35, vol_adjusted_drawdown_exit))

        base_drawdown_warn = settings.drawdown_warn
        vol_adjusted_drawdown_warn = base_drawdown_warn * (asset_vol / 0.15)
        drawdown_warn_threshold = min(-0.05, max(-0.20, vol_adjusted_drawdown_warn))

        status = "green"
        alerts = []
        position_actions = []

        if pnl_pct <= stop_loss_threshold:
            status = "red"
            alerts.append(f"{position.ticker}: досягнуто стоп-лосс ({pnl_pct * 100:+.1f}% проти ліміту {stop_loss_threshold * 100:.1f}%).")
            position_actions.append(f"Продати {position.ticker}: досягнуто стоп-лосс.")
        elif not above_ma:
            status = "red"
            alerts.append(f"{position.ticker}: ціна нижче MA200.")
            position_actions.append(f"Продати {position.ticker}: ціна нижче MA200.")
        elif drawdown <= drawdown_exit_threshold:
            status = "red"
            alerts.append(f"{position.ticker}: просадка становить {drawdown * 100:.1f}% (ліміт виходу {drawdown_exit_threshold * 100:.1f}%).")
            position_actions.append(f"Продати {position.ticker}: досягнуто ліміт просадки.")
        elif drawdown <= drawdown_warn_threshold or pnl_pct < -0.04:
            status = "yellow"
            if drawdown <= drawdown_warn_threshold:
                alerts.append(f"{position.ticker}: попередження про просадку ({drawdown * 100:.1f}% проти попереджувального ліміту {drawdown_warn_threshold * 100:.1f}%).")
            if pnl_pct < -0.04:
                alerts.append(f"{position.ticker}: попередження про незначні втрати ({pnl_pct * 100:+.1f}%).")
            position_actions.append(f"Моніторинг {position.ticker}: перетнуто межу волатильності або попередження.")

        actions.extend(position_actions)
        risks.append(
            PositionRisk(
                ticker=position.ticker,
                status=status,
                current_price=current_price,
                pnl_pct=pnl_pct,
                pnl_abs=pnl_abs,
                drawdown_from_high=drawdown,
                above_ma=above_ma,
                currency=position.currency,
                actions=position_actions,
                alerts=alerts,
            )
        )

    portfolio_pnl = total_value / total_cost - 1 if total_cost > 0 else None
    portfolio_alerts: list[str] = []
    if portfolio_pnl is not None:
        if portfolio_pnl <= settings.portfolio_exit:
            portfolio_alerts.append(f"Портфель впав на {portfolio_pnl * 100:.1f}%: критичний ліміт втрат.")
            actions.append("Вихід у кеш: досягнуто ліміт втрат портфеля.")
        elif portfolio_pnl <= settings.portfolio_warn:
            portfolio_alerts.append(f"Портфель впав на {portfolio_pnl * 100:.1f}%.")

    return RiskReport(
        as_of=prices.index[-1].date(),
        spy_above_ma=spy_above_ma,
        vix=vix,
        market_alerts=market_alerts,
        position_risks=risks,
        portfolio_pnl=portfolio_pnl,
        portfolio_value=total_value,
        portfolio_cost=total_cost,
        actions=sorted(set(actions)),
        currency=report_currency,
        portfolio_alerts=portfolio_alerts,
    )


def format_risk_report(report: RiskReport) -> str:
    status_map = {
        "green": "зелений",
        "yellow": "жовтий",
        "red": "червоний",
        "unknown": "невідомо",
    }

    lines = [
        f"Тижневий моніторинг ризиків ETF | {report.as_of.isoformat()}",
        "",
        f"SPY проти MA200: {'вище' if report.spy_above_ma else 'нижче'}",
        f"VIX: {report.vix:.1f}" if report.vix is not None else "VIX: недоступний",
        "",
        "Позиції:",
    ]

    for risk in report.position_risks:
        status_ukr = status_map.get(risk.status.lower(), risk.status)
        lines.append(
            f"- {risk.ticker}: {status_ukr}, ціна {money(risk.current_price, risk.currency)}, "
            f"P&L {risk.pnl_pct * 100:+.1f}%, просадка {risk.drawdown_from_high * 100:+.1f}%, "
            f"MA200 {'вище' if risk.above_ma else 'нижче'}"
        )

    if report.portfolio_pnl is not None:
        lines.extend(
            [
                "",
                "Портфель:",
                f"- Витрати: {money(report.portfolio_cost, report.currency, decimals=0)}",
                f"- Поточна вартість: {money(report.portfolio_value, report.currency, decimals=0)}",
                f"- P&L: {report.portfolio_pnl * 100:+.1f}%",
            ]
        )

    alerts = report.alerts
    lines.append("")
    if alerts:
        lines.append(f"Сигнали ({len(alerts)}):")
        lines.extend(f"- {alert}" for alert in alerts)
    else:
        lines.append("Немає сигналів. Усі позиції знаходяться в межах встановлених лімітів ризику.")

    if report.actions:
        lines.extend(["", "Дії:"])
        lines.extend(f"- {action}" for action in report.actions)

    return "\n".join(lines)
