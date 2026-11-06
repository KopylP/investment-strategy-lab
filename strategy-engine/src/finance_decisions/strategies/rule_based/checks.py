from __future__ import annotations

from dataclasses import dataclass, field
import re
from typing import Any, Iterator

from .parameters import (
    AllCondition,
    AnyCondition,
    BetweenCondition,
    CompareCondition,
    Condition,
    CrossedCondition,
    IndicatorOperand,
    NotCondition,
    RuleParameters,
    SignalCondition,
)

GUID = re.compile(r"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")

Path = tuple[str | int, ...]


@dataclass(frozen=True)
class RuleProblem:
    path: Path
    code: str
    parameters: dict[str, Any] = field(default_factory=dict)


@dataclass(frozen=True)
class PlacedCondition:
    condition: Condition
    path: Path
    in_market_filter: bool


def problems_in(parameters: RuleParameters) -> list[RuleProblem]:
    problems = _series_problems(parameters)

    for placed in placed_conditions(parameters):
        problems.extend(_condition_problems(placed, parameters))

    problems.extend(_selection_problems(parameters))

    return problems


def referenced_signals(parameters: RuleParameters) -> list[str]:
    found: list[str] = []
    for placed in placed_conditions(parameters):
        if isinstance(placed.condition, SignalCondition) and placed.condition.signal_id not in found:
            found.append(placed.condition.signal_id)

    return found


def placed_conditions(parameters: RuleParameters) -> Iterator[PlacedCondition]:
    if parameters.market_filter is not None:
        yield from _nested(parameters.market_filter.condition, ("marketFilter", "condition"), True)
    if parameters.exit is not None:
        yield from _nested(parameters.exit.condition, ("exit", "condition"), False)
    if parameters.entry is not None:
        yield from _nested(parameters.entry.condition, ("entry", "condition"), False)


def _nested(condition: Condition, path: Path, in_market_filter: bool) -> Iterator[PlacedCondition]:
    yield PlacedCondition(condition, path, in_market_filter)

    if isinstance(condition, (AllCondition, AnyCondition)):
        for index, child in enumerate(condition.conditions):
            yield from _nested(child, path + ("conditions", index), in_market_filter)
    if isinstance(condition, NotCondition):
        yield from _nested(condition.condition, path + ("condition",), in_market_filter)


def _series_problems(parameters: RuleParameters) -> list[RuleProblem]:
    problems = []
    for name, indicator in parameters.indicators.items():
        path = ("indicators", name, "of", "instrumentId")
        is_extra = indicator.of.series == "extra"
        if is_extra and indicator.of.instrument_id is None:
            problems.append(RuleProblem(path, "rule_based.check.extra_needs_instrument", {"name": name}))
        if not is_extra and indicator.of.instrument_id is not None:
            problems.append(RuleProblem(path, "rule_based.check.instrument_without_extra", {"name": name}))

    return problems


def _condition_problems(placed: PlacedCondition, parameters: RuleParameters) -> list[RuleProblem]:
    condition = placed.condition
    references = list(_indicator_references(condition, placed.path))
    problems = [problem for name, path in references for problem in _reference_problems(name, path, placed, parameters)]

    if isinstance(condition, CrossedCondition) and (condition.level is None) == (condition.other is None):
        problems.append(RuleProblem(placed.path, "rule_based.check.crossed_needs_one_side"))
    if isinstance(condition, BetweenCondition) and condition.low > condition.high:
        problems.append(RuleProblem(placed.path + ("low",), "rule_based.check.range_reversed"))
    if isinstance(condition, SignalCondition):
        problems.extend(_signal_problems(condition, placed))

    return problems


def _indicator_references(condition: Condition, path: Path) -> Iterator[tuple[str, Path]]:
    if isinstance(condition, (CompareCondition, BetweenCondition)) and isinstance(condition.left, IndicatorOperand):
        yield condition.left.indicator, path + ("left", "indicator")
    if isinstance(condition, CompareCondition) and isinstance(condition.right, IndicatorOperand):
        yield condition.right.indicator, path + ("right", "indicator")
    if isinstance(condition, CrossedCondition):
        yield condition.indicator, path + ("indicator",)
        if condition.other is not None:
            yield condition.other, path + ("other",)


def _reference_problems(name: str, path: Path, placed: PlacedCondition, parameters: RuleParameters) -> list[RuleProblem]:
    indicator = parameters.indicators.get(name)
    if indicator is None:
        return [RuleProblem(path, "rule_based.check.indicator_unknown", {"name": name})]
    if placed.in_market_filter and indicator.of.series == "fund":
        return [RuleProblem(path, "rule_based.check.fund_in_market_filter", {"name": name})]

    return []


def _signal_problems(condition: SignalCondition, placed: PlacedCondition) -> list[RuleProblem]:
    problems = []
    if not GUID.match(condition.signal_id):
        problems.append(RuleProblem(placed.path + ("signalId",), "rule_based.check.signal_id_format", {"id": condition.signal_id}))
    if placed.in_market_filter and condition.scope == "thisFund":
        problems.append(RuleProblem(placed.path + ("scope",), "rule_based.check.this_fund_signal_in_market_filter"))

    return problems


def _selection_problems(parameters: RuleParameters) -> list[RuleProblem]:
    rank_by = parameters.selection.rank_by
    if rank_by is None or rank_by in parameters.indicators:
        return []

    return [RuleProblem(("selection", "rankBy"), "rule_based.check.rank_by_unknown", {"name": rank_by})]
