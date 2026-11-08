"""Adapter from the API contract to the existing scanner and risk monitor.

The engine decides *targets* (which ETFs, what weight, how much of the contribution to
invest); turning them into orders is the backend's job (the port of plan.py).
"""

from __future__ import annotations

from importlib.metadata import version
from pathlib import Path

import pandas as pd

from finance_decisions.api.contracts import (
    ColumnFormat,
    EvaluationContext,
    InstrumentScore,
    MarketRegime,
    PortfolioRisk,
    PositionRisk,
    Rationale,
    RationaleColumn,
    ReportDefinition,
    ReportFrequency,
    ReportMode,
    ReportSchedule,
    RiskAssessment,
    RiskSignal,
    RiskSignalKind,
    SeriesRole,
    SignalKind,
    StrategyDecision,
    TargetWeight,
    Weekday,
)
from finance_decisions.helpers import holdings_by_value, is_na, latest_value
from finance_decisions.messages import catalogs_of, msg
from finance_decisions.models import Position
from finance_decisions.strategies.errors import ContextError
from .parameters import EtfMomentumParameters
from .risk import analyze_portfolio_risk
from finance_decisions.market_data import BENCHMARK_SYMBOL, MARKET_SYMBOLS, VOLATILITY_INDEX_SYMBOL
from .scanner import STATUS_FAILED, scan_etfs

STRATEGY_CODE = "etf-momentum"
MESSAGES = catalogs_of(Path(__file__).resolve().parent)

# The reports the service makes for a sub-portfolio with this strategy, at these times unless the user sets their own for the account.
REPORTS = (
    ReportDefinition(
        code="rebalance",
        name=msg("etf_momentum.report.rebalance.name"),
        description=msg("etf_momentum.report.rebalance.description"),
        mode=ReportMode.REBALANCE,
        schedule=ReportSchedule(frequency=ReportFrequency.MONTHLY, day=1, time="20:00"),
    ),
    ReportDefinition(
        code="risk-review",
        name=msg("etf_momentum.report.risk_review.name"),
        description=msg("etf_momentum.report.risk_review.description"),
        mode=ReportMode.RISK_REVIEW,
        schedule=ReportSchedule(frequency=ReportFrequency.WEEKLY, weekday=Weekday.SUNDAY, time="19:00"),
        signal_kinds=[SignalKind.VOLATILITY_INDEX, SignalKind.ACCOUNT_DRAWDOWN, SignalKind.POSITION_DRAWDOWN],
    ),
)

# The scanner and risk monitor read the market regime from these fixed columns.
_ROLE_COLUMNS = {SeriesRole.BENCHMARK: BENCHMARK_SYMBOL, SeriesRole.VOLATILITY_INDEX: VOLATILITY_INDEX_SYMBOL}


def engine_version() -> str:
    return f"{STRATEGY_CODE}/{version('finance-decisions')}"


def price_frame(context: EvaluationContext) -> pd.DataFrame:
    """Adjusted closes up to ``asOf``; benchmark and VIX under the columns the scanner expects."""
    columns: dict[str, pd.Series] = {}
    for series in context.market_series:
        if series.role == SeriesRole.INSTRUMENT and series.key in MARKET_SYMBOLS:
            raise ContextError(f"instrument key '{series.key}' is reserved; use the benchmark/volatilityIndex roles")
        column = _ROLE_COLUMNS.get(series.role, series.key)
        if column in columns:
            raise ContextError(f"more than one series has role '{series.role.value}'")
        columns[column] = pd.Series(series.adjusted_close, index=pd.to_datetime(series.dates), dtype=float)

    missing = [role.value for role, column in _ROLE_COLUMNS.items() if column not in columns]
    if missing:
        raise ContextError(f"missing series with role: {', '.join(missing)}")

    frame = pd.DataFrame(columns).sort_index()
    frame = frame.loc[: pd.Timestamp(context.as_of)].dropna(how="all")
    if frame.empty:
        raise ContextError(f"no prices on or before {context.as_of}")
    return frame


def decide(context: EvaluationContext, parameters: EtfMomentumParameters) -> StrategyDecision:
    frame = price_frame(context)
    names = {member.key: member.name or member.key for member in context.universe}
    groups = {member.key: member.group or member.key for member in context.universe}
    holdings = {holding.key: holding.quantity for holding in context.sub_portfolio.holdings}
    latest = {column: latest_value(frame[column]) for column in frame.columns}

    settings = parameters.scanner.to_settings()
    scan = scan_etfs(
        frame,
        settings=settings,
        etfs=[member.key for member in context.universe],
        current_positions=holdings_by_value(holdings, latest),
        groups=groups,
        names=names,
        cash_etf=context.cash_instrument_key,
        now=pd.Timestamp(context.as_of),
    )

    targets = [TargetWeight(key=key, weight=float(scan.eligible.loc[key, "Weight"])) for key in scan.eligible.index]
    scores = [_score(key, row) for key, row in scan.table.iterrows()]
    return StrategyDecision(
        strategy_code=STRATEGY_CODE,
        engine_version=engine_version(),
        as_of=scan.as_of,
        invest_fraction=1.0 if scan.market_ok else parameters.weak_market_invest_pct,
        targets=targets,
        market=MarketRegime(
            is_favorable=scan.market_ok,
            benchmark_price=scan.spy_price,
            benchmark_moving_average=scan.spy_ma,
            volatility_index=scan.vix,
            reasons=scan.market_reasons,
        ),
        rationale=Rationale(
            summary=_summary(scan, targets),
            title=msg("etf_momentum.rationale.title"),
            instrument_label="ETF",
            columns=_columns(settings.lookback_months),
            risk_free_rate=scan.risk_free_rate,
            scores=scores,
        ),
        warnings=[f"{score.name}: {score.reason}" for score in scores if score.status == STATUS_FAILED and score.price is None],
    )


def assess_risk(context: EvaluationContext, parameters: EtfMomentumParameters) -> RiskAssessment:
    frame = price_frame(context)
    names = {member.key: member.name or member.key for member in context.universe}
    currency = context.sub_portfolio.currency.upper()
    lots = [
        Position(holding.key, lot.price, lot.quantity, lot.acquired_on.isoformat() if lot.acquired_on else None, currency)
        for holding in context.sub_portfolio.holdings
        for lot in holding.lots
    ]
    report = analyze_portfolio_risk(
        frame,
        lots,
        settings=parameters.risk.to_settings(),
        cash=context.sub_portfolio.cash,
        cash_etf=context.cash_instrument_key,
        names=names,
    )

    positions = [
        PositionRisk(
            key=risk.ticker,
            status=risk.status,
            quantity=risk.shares,
            average_cost=risk.entry_price,
            current_price=risk.current_price,
            value=risk.value,
            pnl_pct=risk.pnl_pct,
            drawdown_from_high=risk.drawdown_from_high,
            above_moving_average=risk.above_ma,
            moving_average=risk.ma_value,
            exit_price=risk.exit_price,
            exit_rule=risk.exit_rule,
            exit_distance=risk.exit_distance,
            stop_price=risk.stop_price,
            ma_exit_price=risk.ma_exit_price,
            trail_exit_price=risk.trail_exit_price,
            alerts=risk.alerts,
            sell=risk.sell,
        )
        for risk in report.position_risks
    ]
    return RiskAssessment(
        strategy_code=STRATEGY_CODE,
        engine_version=engine_version(),
        as_of=report.as_of,
        status=report.status,
        market_alerts=report.market_alerts,
        positions=positions,
        portfolio=PortfolioRisk(
            value=report.portfolio_value,
            cost=report.portfolio_cost,
            pnl_pct=report.portfolio_pnl,
            drawdown=report.portfolio_drawdown,
            peak_date=report.portfolio_peak_date,
            exit_limit_hit=report.portfolio_exit_hit,
            alerts=report.portfolio_alerts,
        ),
        signals=_signals(report),
        actions=report.actions,
    )


def _signals(report) -> list[RiskSignal]:
    signals = []
    for risk in report.position_risks:
        if risk.status == "cash":
            continue
        if risk.sell or (report.portfolio_exit_hit and risk.status != "unknown"):
            reason = risk.alerts[0] if risk.sell and risk.alerts else msg("etf_momentum.scan.failures", items=report.portfolio_alerts)
            signals.append(RiskSignal(key=risk.ticker, kind=RiskSignalKind.EXIT, reason=reason, exit_price=risk.exit_price))
        elif risk.alerts:
            signals.append(RiskSignal(key=risk.ticker, kind=RiskSignalKind.WARN, reason=msg("etf_momentum.scan.failures", items=risk.alerts), exit_price=risk.exit_price))
    return signals


def _columns(lookback_months: int) -> list[RationaleColumn]:
    """The "why" table of the report: the scanner's metrics and filters, in the order a reader checks them."""
    return [
        RationaleColumn(id="momentum", label=msg("etf_momentum.column.momentum"), format=ColumnFormat.SIGNED_PERCENT, compact=True,
                        hint=msg("etf_momentum.column.momentum_hint", months=lookback_months)),
        RationaleColumn(id="return1m", label=msg("etf_momentum.column.return1m"), format=ColumnFormat.SIGNED_PERCENT),
        RationaleColumn(id="return3m", label=msg("etf_momentum.column.return3m"), format=ColumnFormat.SIGNED_PERCENT),
        RationaleColumn(id="return12m", label=msg("etf_momentum.column.return12m"), format=ColumnFormat.SIGNED_PERCENT),
        RationaleColumn(id="volatility", label=msg("etf_momentum.column.volatility"), format=ColumnFormat.PERCENT),
        RationaleColumn(id="ma200", label="MA200", format=ColumnFormat.TREND, compact=True,
                        true_hint=msg("etf_momentum.column.ma200_above"), false_hint=msg("etf_momentum.column.ma200_below")),
    ]


def _score(key: str, row: pd.Series) -> InstrumentScore:
    def number(column: str) -> float | None:
        value = row[column]
        return None if is_na(value) else float(value)

    rank = number("Rank")
    values = {
        "momentum": number("Momentum"),
        "return1m": number("Return 1M"),
        "return3m": number("Return 3M"),
        "return12m": number("Return 12M"),
        "volatility": number("Volatility"),
        "ma200": bool(row["Above MA200"]),
    }
    return InstrumentScore(
        key=str(key),
        name=str(row["Name"]),
        group=str(row["Group"]),
        price=number("Price"),
        momentum=number("Momentum"),
        return_1m=number("Return 1M"),
        return_3m=number("Return 3M"),
        return_12m=number("Return 12M"),
        volatility=number("Volatility"),
        sharpe=number("Sharpe"),
        drawdown=number("Drawdown"),
        above_moving_average=bool(row["Above MA200"]),
        last_date=None if is_na(row["Last Date"]) else row["Last Date"],
        rank=None if rank is None else int(rank),
        status=str(row["Status"]),
        reason=str(row["Reason"]),
        weight=number("Weight"),
        values=values,
    )


def _summary(scan, targets: list[TargetWeight]) -> str:
    market = msg("etf_momentum.summary.market_ok") if scan.market_ok else msg("etf_momentum.summary.market_weak")
    if not targets:
        return msg("etf_momentum.summary.nothing", market=market)
    names = ", ".join(scan.table.loc[target.key, "Name"] for target in targets)
    return msg("etf_momentum.summary.picked", count=len(targets), names=names, market=market)
