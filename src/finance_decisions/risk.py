from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date

import pandas as pd

from finance_decisions.config import RiskSettings
from finance_decisions.helpers import (
    is_na,
    latest_moving_average,
    latest_rolling_high,
    latest_value,
    money,
    safe_division,
)
from finance_decisions.models import Position


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
        market_alerts.append("SPY is below MA200: broad market trend is weak.")
        actions.append("Consider moving portfolio risk down or raising cash.")

    if vix is not None:
        if vix > settings.vix_exit:
            market_alerts.append(f"VIX is {vix:.1f}: market stress is high.")
            actions.append("Reduce exposure during elevated volatility.")
        elif vix > settings.vix_warn:
            market_alerts.append(f"VIX is {vix:.1f}: volatility is elevated.")

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
                    alerts=[f"{position.ticker}: price data is unavailable."],
                )
            )
            continue

        price_series = prices[position.ticker]
        current_price = latest_value(price_series)
        moving_average = latest_moving_average(price_series, settings.ma_period)
        ticker_high = latest_rolling_high(price_series, 252)
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
                    alerts=[f"{position.ticker}: price data is unavailable."],
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

        status = "green"
        alerts: list[str] = []
        position_actions: list[str] = []

        if pnl_pct <= settings.stop_loss_pct:
            status = "red"
            alerts.append(f"{position.ticker}: stop loss reached ({pnl_pct * 100:+.1f}%).")
            position_actions.append(f"Sell {position.ticker}: stop loss reached.")
        elif not above_ma:
            status = "red"
            alerts.append(f"{position.ticker}: price is below MA200.")
            position_actions.append(f"Sell {position.ticker}: below MA200.")
        elif drawdown <= settings.drawdown_exit:
            status = "red"
            alerts.append(f"{position.ticker}: drawdown is {drawdown * 100:.1f}%.")
            position_actions.append(f"Sell {position.ticker}: drawdown limit reached.")
        elif drawdown <= settings.drawdown_warn or pnl_pct < -0.04:
            status = "yellow"
            alerts.append(f"{position.ticker}: weakness detected ({pnl_pct * 100:+.1f}%).")

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
            portfolio_alerts.append(f"Portfolio is down {portfolio_pnl * 100:.1f}%: critical loss limit.")
            actions.append("Move to cash: portfolio loss limit reached.")
        elif portfolio_pnl <= settings.portfolio_warn:
            portfolio_alerts.append(f"Portfolio is down {portfolio_pnl * 100:.1f}%.")

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
    lines = [
        f"ETF Risk Monitor | {report.as_of.isoformat()}",
        "",
        f"SPY vs MA200: {'above' if report.spy_above_ma else 'below'}",
        f"VIX: {report.vix:.1f}" if report.vix is not None else "VIX: unavailable",
        "",
        "Positions:",
    ]

    for risk in report.position_risks:
        lines.append(
            f"- {risk.ticker}: {risk.status}, price {money(risk.current_price, risk.currency)}, "
            f"P&L {risk.pnl_pct * 100:+.1f}%, drawdown {risk.drawdown_from_high * 100:+.1f}%, "
            f"MA200 {'above' if risk.above_ma else 'below'}"
        )

    if report.portfolio_pnl is not None:
        lines.extend(
            [
                "",
                "Portfolio:",
                f"- Cost: {money(report.portfolio_cost, report.currency, decimals=0)}",
                f"- Value: {money(report.portfolio_value, report.currency, decimals=0)}",
                f"- P&L: {report.portfolio_pnl * 100:+.1f}%",
            ]
        )

    alerts = report.alerts
    lines.append("")
    if alerts:
        lines.append(f"Alerts ({len(alerts)}):")
        lines.extend(f"- {alert}" for alert in alerts)
    else:
        lines.append("No alerts. Positions are inside the configured risk rules.")

    if report.actions:
        lines.extend(["", "Actions:"])
        lines.extend(f"- {action}" for action in report.actions)

    return "\n".join(lines)



