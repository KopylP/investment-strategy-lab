from __future__ import annotations

from importlib.metadata import version
from pathlib import Path

from finance_decisions.api.contracts import (
    EvaluationContext,
    InstrumentScore,
    MarketRegime,
    PortfolioRisk,
    Rationale,
    ReportDefinition,
    ReportFrequency,
    ReportMode,
    ReportSchedule,
    RiskAssessment,
    StrategyDecision,
    TargetWeight,
    UniverseMember,
)
from finance_decisions.messages import catalog_of, msg
from .parameters import BuyAndHoldParameters

STRATEGY_CODE = "buy-and-hold"
MESSAGES = catalog_of(Path(__file__).resolve().parent)
STATUS_SELECTED = "selected"
RISK_STATUS_OK = "ok"

REPORTS = (
    ReportDefinition(
        code="rebalance",
        name=msg("buy_and_hold.report.rebalance.name"),
        description=msg("buy_and_hold.report.rebalance.description"),
        mode=ReportMode.REBALANCE,
        schedule=ReportSchedule(frequency=ReportFrequency.MONTHLY, day=1, time="20:00"),
    ),
)


def engine_version() -> str:
    return f"{STRATEGY_CODE}/{version('finance-decisions')}"


def decide(context: EvaluationContext, parameters: BuyAndHoldParameters) -> StrategyDecision:
    weight = 1.0 / len(context.universe)
    prices = _latest_prices(context)
    targets = [TargetWeight(key=member.key, weight=weight) for member in context.universe]
    scores = [_score(member, rank, weight, prices) for rank, member in enumerate(context.universe, start=1)]

    return StrategyDecision(
        strategy_code=STRATEGY_CODE,
        engine_version=engine_version(),
        as_of=context.as_of,
        invest_fraction=1.0,
        targets=targets,
        market=_any_market(),
        rationale=Rationale(summary=msg("buy_and_hold.rationale.summary"), title=msg("buy_and_hold.rationale.title"), scores=scores),
    )


def assess_risk(context: EvaluationContext, parameters: BuyAndHoldParameters) -> RiskAssessment:
    return RiskAssessment(
        strategy_code=STRATEGY_CODE,
        engine_version=engine_version(),
        as_of=context.as_of,
        status=RISK_STATUS_OK,
        market_alerts=[],
        positions=[],
        portfolio=PortfolioRisk(
            value=context.sub_portfolio.cash,
            cost=context.sub_portfolio.cash,
            pnl_pct=None,
            drawdown=None,
            peak_date=None,
            exit_limit_hit=False,
            alerts=[],
        ),
        signals=[],
        actions=[],
    )


def _latest_prices(context: EvaluationContext) -> dict[str, float]:
    prices: dict[str, float] = {}
    for series in context.market_series:
        known = [close for day, close in zip(series.dates, series.adjusted_close) if day <= context.as_of and close is not None]
        if known:
            prices[series.key] = known[-1]

    return prices


def _score(member: UniverseMember, rank: int, weight: float, prices: dict[str, float]) -> InstrumentScore:
    return InstrumentScore(
        key=member.key,
        name=member.name or member.key,
        group=member.group or member.key,
        status=STATUS_SELECTED,
        reason=msg("buy_and_hold.score.held"),
        rank=rank,
        weight=weight,
        price=prices.get(member.key),
    )


def _any_market() -> MarketRegime:
    return MarketRegime(
        is_favorable=True,
        benchmark_price=None,
        benchmark_moving_average=None,
        volatility_index=None,
        reasons=[msg("buy_and_hold.market.ignored")],
    )
