from __future__ import annotations

from dataclasses import dataclass
from importlib.metadata import version
from pathlib import Path

from finance_decisions.api.contracts import (
    EvaluationContext,
    MarketRegime,
    ReportDefinition,
    ReportFrequency,
    ReportMode,
    ReportSchedule,
    StrategyDecision,
    TargetWeight,
    Weekday,
)
from finance_decisions.helpers import latest_value
from finance_decisions.messages import catalogs_of, msg
from finance_decisions.strategies.errors import ContextError
from .conditions import ConditionReader
from .parameters import RuleParameters
from .rationale import rationale_of
from .selection import judge_funds
from .series_book import SeriesBook
from .weights import WeightPlan, weights_for

STRATEGY_CODE = "rule-based"
MESSAGES = catalogs_of(Path(__file__).resolve().parent)

REPORTS = (
    ReportDefinition(
        code="rebalance",
        name=msg("rule_based.report.rebalance.name"),
        description=msg("rule_based.report.rebalance.description"),
        mode=ReportMode.REBALANCE,
        schedule=ReportSchedule(frequency=ReportFrequency.MONTHLY, day=1, time="20:00"),
    ),
    ReportDefinition(
        code="risk-review",
        name=msg("rule_based.report.risk-review.name"),
        description=msg("rule_based.report.risk-review.description"),
        mode=ReportMode.RISK_REVIEW,
        schedule=ReportSchedule(frequency=ReportFrequency.WEEKLY, weekday=Weekday.SUNDAY, time="20:00"),
        signal_kinds=[],
    ),
)


@dataclass(frozen=True)
class MarketView:
    favorable: bool
    invest_fraction: float
    reasons: list[str]
    holdings_share: float = 1.0


def engine_version() -> str:
    return f"{STRATEGY_CODE}/{version('finance-decisions')}"


def decide(context: EvaluationContext, parameters: RuleParameters) -> StrategyDecision:
    book = SeriesBook.from_context(context)
    reader = ConditionReader(parameters, book, context.signals)
    prices = book.funds_frame()
    if prices.empty:
        raise ContextError(f"no instrument prices on or before {context.as_of}")

    market = _market_view(parameters, reader)
    held = {holding.key for holding in context.sub_portfolio.holdings}
    verdicts = judge_funds(parameters, reader, context.universe, held, prices)
    picks = [verdict.key for verdict in verdicts if verdict.selected]
    plan = weights_for(picks, prices, parameters.weights, parameters.selection.top)
    targets = _targets_within(plan, market.holdings_share)

    return StrategyDecision(
        strategy_code=STRATEGY_CODE,
        engine_version=engine_version(),
        as_of=prices.index[-1].date(),
        invest_fraction=market.invest_fraction,
        targets=targets,
        market=_regime(book, market, plan, parameters),
        rationale=rationale_of(parameters, reader, verdicts, plan),
    )


def _market_view(parameters: RuleParameters, reader: ConditionReader) -> MarketView:
    market_filter = parameters.market_filter
    if market_filter is None:
        return MarketView(True, 1.0, [msg("rule_based.market.no_filter")])

    outcome = reader.read(market_filter.condition, None)
    if outcome.passed:
        return MarketView(True, 1.0, [msg("rule_based.market.favorable", items=outcome.reasons)])

    share = market_filter.invest_when_unfavorable
    if market_filter.reduces_holdings:
        reason = msg("rule_based.market.unfavorable", items=outcome.reasons, share=share)
        return MarketView(False, 1.0, [reason], holdings_share=share)

    reason = msg("rule_based.market.unfavorable_new_money", items=outcome.reasons, share=share)

    return MarketView(False, share, [reason])


def _targets_within(plan: WeightPlan, holdings_share: float) -> list[TargetWeight]:
    return [TargetWeight(key=key, weight=weight * holdings_share) for key, weight in plan.weights.items()]


def _regime(book: SeriesBook, market: MarketView, plan: WeightPlan, parameters: RuleParameters) -> MarketRegime:
    benchmark = book.benchmark_or_none()
    price = latest_value(benchmark) if benchmark is not None else None

    return MarketRegime(
        is_favorable=market.favorable,
        benchmark_price=price,
        benchmark_moving_average=None,
        volatility_index=None,
        reasons=market.reasons + _weight_reasons(plan, parameters),
    )


def _weight_reasons(plan: WeightPlan, parameters: RuleParameters) -> list[str]:
    if not plan.weights:
        return [msg("rule_based.market.nothing_qualified")]
    if plan.share < 1.0:
        target = parameters.weights.target_volatility
        return [msg("rule_based.market.exposure_reduced", volatility=plan.volatility, target=target, share=plan.share)]

    return []
