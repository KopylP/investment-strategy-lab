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
    Rationale,
    RationaleColumn,
    ReportDefinition,
    ReportFrequency,
    ReportMode,
    ReportSchedule,
    RiskAssessment,
    SeriesRole,
    StrategyDecision,
    TargetWeight,
)
from finance_decisions.helpers import latest_moving_average, latest_value
from finance_decisions.messages import catalog_of, msg
from finance_decisions.strategies.errors import ContextError
from .exposure import Exposure, exposure_for
from .fund_reading import FundReading, read_fund
from .parameters import AdaptiveMomentumParameters
from .selection import Verdict, select

STRATEGY_CODE = "adaptive-momentum"
MESSAGES = catalog_of(Path(__file__).resolve().parent)
RISK_STATUS_OK = "ok"
BENCHMARK_TREND_DAYS = 200

REPORTS = (
    ReportDefinition(
        code="rebalance",
        name=msg("adaptive_momentum.report.rebalance.name"),
        description=msg("adaptive_momentum.report.rebalance.description"),
        mode=ReportMode.REBALANCE,
        schedule=ReportSchedule(frequency=ReportFrequency.MONTHLY, day=1, time="20:00"),
    ),
)


def engine_version() -> str:
    return f"{STRATEGY_CODE}/{version('finance-decisions')}"


def decide(context: EvaluationContext, parameters: AdaptiveMomentumParameters) -> StrategyDecision:
    rules = parameters.scanner.to_rules()
    prices = _instrument_prices(context)

    readings = _readings(context, prices, rules.trend_days)
    verdicts = select(readings, prices, rules)

    slots = {verdict.reading.key: 1.0 / rules.top_n for verdict in verdicts if verdict.selected}
    exposure = exposure_for(prices, slots, parameters.target_volatility, parameters.volatility_window_days)
    weights = {key: weight * exposure.share for key, weight in slots.items()}

    return StrategyDecision(
        strategy_code=STRATEGY_CODE,
        engine_version=engine_version(),
        as_of=_last_day(prices),
        invest_fraction=1.0,
        targets=[TargetWeight(key=key, weight=weight) for key, weight in weights.items()],
        market=_market(context, exposure, has_picks=bool(slots)),
        rationale=_rationale(verdicts, weights, exposure, rules.trend_days),
    )


def assess_risk(context: EvaluationContext, parameters: AdaptiveMomentumParameters) -> RiskAssessment:
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


def _instrument_prices(context: EvaluationContext) -> pd.DataFrame:
    columns = {series.key: _until_as_of(series, context) for series in context.market_series if series.role == SeriesRole.INSTRUMENT}
    frame = pd.DataFrame(columns).sort_index().dropna(how="all")
    if frame.empty:
        raise ContextError(f"no instrument prices on or before {context.as_of}")

    return frame


def _until_as_of(series, context: EvaluationContext) -> pd.Series:
    prices = pd.Series(series.adjusted_close, index=pd.to_datetime(series.dates), dtype=float).sort_index()

    return prices.loc[: pd.Timestamp(context.as_of)]


def _readings(context: EvaluationContext, prices: pd.DataFrame, trend_days: int) -> list[FundReading]:
    readings = []
    for member in context.universe:
        series = prices[member.key] if member.key in prices.columns else pd.Series(dtype=float)
        readings.append(read_fund(member.key, member.name or member.key, member.group or member.key, series, trend_days))

    return readings


def _last_day(prices: pd.DataFrame):
    return prices.index[-1].date()


def _market(context: EvaluationContext, exposure: Exposure, has_picks: bool) -> MarketRegime:
    benchmark = _benchmark_prices(context)
    price = latest_value(benchmark) if benchmark is not None else None
    average = latest_moving_average(benchmark, BENCHMARK_TREND_DAYS) if benchmark is not None else None

    return MarketRegime(
        is_favorable=True,
        benchmark_price=price,
        benchmark_moving_average=average,
        volatility_index=None,
        reasons=[_exposure_reason(exposure, has_picks)],
    )


def _benchmark_prices(context: EvaluationContext) -> pd.Series | None:
    found = [series for series in context.market_series if series.role == SeriesRole.BENCHMARK]
    if not found:
        return None

    return _until_as_of(found[0], context).dropna()


def _exposure_reason(exposure: Exposure, has_picks: bool) -> str:
    if not has_picks:
        return msg("adaptive_momentum.market.nothing_qualified")
    if exposure.reduced:
        return msg("adaptive_momentum.market.exposure_reduced", volatility=exposure.volatility, target=exposure.target, share=exposure.share)

    return msg("adaptive_momentum.market.exposure_full", target=exposure.target)


def _rationale(verdicts: list[Verdict], weights: dict[str, float], exposure: Exposure, trend_days: int) -> Rationale:
    return Rationale(
        summary=_summary(verdicts, exposure),
        title=msg("adaptive_momentum.rationale.title"),
        instrument_label="ETF",
        columns=_columns(trend_days),
        scores=[_score(verdict, weights.get(verdict.reading.key)) for verdict in verdicts],
    )


def _summary(verdicts: list[Verdict], exposure: Exposure) -> str:
    names = [verdict.reading.name for verdict in verdicts if verdict.selected]
    if not names:
        return msg("adaptive_momentum.summary.nothing")

    return msg("adaptive_momentum.summary.picked", count=len(names), names=names, share=exposure.share)


def _columns(trend_days: int) -> list[RationaleColumn]:
    return [
        RationaleColumn(id="momentum", label=msg("adaptive_momentum.column.momentum"), format=ColumnFormat.SIGNED_PERCENT, compact=True,
                        hint=msg("adaptive_momentum.column.momentum_hint")),
        RationaleColumn(id="return3m", label=msg("adaptive_momentum.column.return3m"), format=ColumnFormat.SIGNED_PERCENT),
        RationaleColumn(id="return6m", label=msg("adaptive_momentum.column.return6m"), format=ColumnFormat.SIGNED_PERCENT),
        RationaleColumn(id="return12m", label=msg("adaptive_momentum.column.return12m"), format=ColumnFormat.SIGNED_PERCENT),
        RationaleColumn(id="volatility", label=msg("adaptive_momentum.column.volatility"), format=ColumnFormat.PERCENT),
        RationaleColumn(id="trend", label=msg("adaptive_momentum.column.trend", days=trend_days), format=ColumnFormat.TREND, compact=True,
                        true_hint=msg("adaptive_momentum.column.trend_above", days=trend_days),
                        false_hint=msg("adaptive_momentum.column.trend_below", days=trend_days)),
    ]


def _score(verdict: Verdict, weight: float | None) -> InstrumentScore:
    reading = verdict.reading
    values = {
        "momentum": reading.momentum,
        "return3m": reading.returns.get(3),
        "return6m": reading.returns.get(6),
        "return12m": reading.returns.get(12),
        "volatility": reading.volatility,
        "trend": reading.above_trend,
    }

    return InstrumentScore(
        key=reading.key,
        name=reading.name,
        group=reading.group,
        status=verdict.status,
        reason=verdict.reason,
        rank=verdict.rank,
        momentum=reading.momentum,
        weight=weight,
        price=reading.price,
        return_3m=reading.returns.get(3),
        return_12m=reading.returns.get(12),
        volatility=reading.volatility,
        above_moving_average=reading.above_trend,
        last_date=reading.last_date,
        values=values,
    )
