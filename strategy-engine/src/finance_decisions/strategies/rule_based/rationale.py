from __future__ import annotations

from finance_decisions.api.contracts import ColumnFormat, InstrumentScore, Rationale, RationaleColumn
from finance_decisions.messages import msg
from .checks import placed_conditions
from .conditions import ConditionReader
from .parameters import BetweenCondition, CompareCondition, CrossedCondition, IndicatorOperand, RuleParameters
from .selection import FundVerdict
from .weights import WeightPlan


def rationale_of(parameters: RuleParameters, reader: ConditionReader, verdicts: list[FundVerdict], plan: WeightPlan) -> Rationale:
    names = fund_indicators_used(parameters)

    return Rationale(
        summary=_summary(verdicts, plan),
        title=msg("rule_based.rationale.title"),
        columns=[_column(parameters, reader, name, position) for position, name in enumerate(names)],
        scores=[_score(parameters, reader, verdict, plan, names) for verdict in verdicts],
    )


def fund_indicators_used(parameters: RuleParameters) -> list[str]:
    names: list[str] = []
    for placed in placed_conditions(parameters):
        if not placed.in_market_filter:
            names.extend(_names_in(placed.condition))
    if parameters.selection.rank_by is not None:
        names.append(parameters.selection.rank_by)

    fund_names = [name for name in names if parameters.indicators[name].of.series == "fund"]

    return list(dict.fromkeys(fund_names))


def _names_in(condition) -> list[str]:
    if isinstance(condition, CrossedCondition):
        return [condition.indicator] + ([condition.other] if condition.other else [])
    if isinstance(condition, (CompareCondition, BetweenCondition)):
        operands = [condition.left] + ([condition.right] if isinstance(condition, CompareCondition) else [])
        return [operand.indicator for operand in operands if isinstance(operand, IndicatorOperand)]

    return []


def _column(parameters: RuleParameters, reader: ConditionReader, name: str, position: int) -> RationaleColumn:
    definition = parameters.indicators[name]
    settings = definition.model_dump(exclude={"kind", "of"})

    return RationaleColumn(
        id=name,
        label=msg(f"rule_based.indicator.{definition.kind}.column", **settings),
        format=ColumnFormat(reader.column_format(name)),
        hint=msg(f"rule_based.indicator.{definition.kind}.hint"),
        compact=position == 0,
    )


def _score(parameters: RuleParameters, reader: ConditionReader, verdict: FundVerdict, plan: WeightPlan, names: list[str]) -> InstrumentScore:
    values = {name: reader.value_of(name, verdict.key) for name in names}

    return InstrumentScore(
        key=verdict.key,
        name=verdict.name,
        group=verdict.group,
        status=verdict.status,
        reason=verdict.reason,
        rank=verdict.rank,
        momentum=_main_score(parameters, reader, verdict),
        weight=plan.weights.get(verdict.key),
        price=reader.last_price(verdict.key),
        values=values,
    )


def _main_score(parameters: RuleParameters, reader: ConditionReader, verdict: FundVerdict) -> float | None:
    rank_by = parameters.selection.rank_by
    if rank_by is None or reader.column_format(rank_by) != ColumnFormat.SIGNED_PERCENT.value:
        return None

    return verdict.score


def _summary(verdicts: list[FundVerdict], plan: WeightPlan) -> str:
    names = [verdict.name for verdict in verdicts if verdict.selected]
    if not names:
        return msg("rule_based.summary.nothing")

    return msg("rule_based.summary.picked", count=len(names), names=names, share=plan.share)
