from __future__ import annotations

from dataclasses import dataclass
import math

import pandas as pd

from finance_decisions.api.contracts import SignalState
from finance_decisions.messages import msg
from finance_decisions.strategies.errors import ContextError
from .indicators import history_needed, indicator_series
from .parameters import (
    AllCondition,
    AnyCondition,
    BetweenCondition,
    CompareCondition,
    Condition,
    CrossedCondition,
    IndicatorOperand,
    NotCondition,
    Operand,
    RuleParameters,
    SignalCondition,
)
from .series_book import SeriesBook

COMPARISONS = {
    "gt": lambda left, right: left > right,
    "lt": lambda left, right: left < right,
    "gte": lambda left, right: left >= right,
    "lte": lambda left, right: left <= right,
}


@dataclass(frozen=True)
class Outcome:
    passed: bool
    reasons: list[str]


class ConditionReader:
    def __init__(self, parameters: RuleParameters, book: SeriesBook, signals: list[SignalState] | None) -> None:
        self._parameters = parameters
        self._book = book
        self._signals = {signal.id: signal for signal in signals or []}
        self._has_signals = signals is not None
        self._cache: dict[tuple[str, str | None], pd.Series] = {}

    def read(self, condition: Condition, fund_key: str | None) -> Outcome:
        if isinstance(condition, CompareCondition):
            return self._compare(condition, fund_key)
        if isinstance(condition, BetweenCondition):
            return self._between(condition, fund_key)
        if isinstance(condition, CrossedCondition):
            return self._crossed(condition, fund_key)
        if isinstance(condition, SignalCondition):
            return self._signal(condition, fund_key)
        if isinstance(condition, AllCondition):
            return self._all(condition, fund_key)
        if isinstance(condition, AnyCondition):
            return self._any(condition, fund_key)
        if isinstance(condition, NotCondition):
            return self._not(condition, fund_key)

        raise ValueError(f"unknown condition kind '{condition.kind}'")

    def value_of(self, name: str, fund_key: str | None) -> float | None:
        series = self.series_of(name, fund_key)

        return _known(series.iloc[-1]) if len(series) else None

    def series_of(self, name: str, fund_key: str | None) -> pd.Series:
        definition = self._parameters.indicators[name]
        cache_key = (name, fund_key if definition.of.series == "fund" else None)
        if cache_key not in self._cache:
            prices = self._book.of(definition.of, fund_key)
            benchmark = self._book.benchmark() if definition.kind == "relativeStrength" else None
            self._cache[cache_key] = indicator_series(definition, prices, benchmark)

        return self._cache[cache_key]

    def column_format(self, name: str) -> str:
        definition = self._parameters.indicators[name]

        return definition.model_config["json_schema_extra"]["x-column-format"]

    def shown(self, name: str, value: float) -> str:
        return msg(f"rule_based.value.{self.column_format(name)}", value=value)

    def short_history(self, name: str, extra_days: int = 0) -> Outcome:
        days = history_needed(self._parameters.indicators[name]) + extra_days

        return Outcome(False, [msg("rule_based.reason.short_history", name=name, days=days)])

    def _compare(self, condition: CompareCondition, fund_key: str | None) -> Outcome:
        left = self._operand_value(condition.left, fund_key)
        right = self._operand_value(condition.right, fund_key)
        missing = self._first_missing([(condition.left, left), (condition.right, right)])
        if missing is not None:
            return self.short_history(missing)

        passed = COMPARISONS[condition.operator](left, right)
        format_name = _indicator_of(condition.left) or _indicator_of(condition.right)
        reason = msg(
            "rule_based.reason.compare",
            left=self._operand_label(condition.left, left, format_name),
            leftValue=self.shown(format_name, left) if format_name else left,
            operator=msg(f"rule_based.choice.operator.{condition.operator}"),
            right=self._operand_label(condition.right, right, format_name),
            verdict=_verdict(passed),
        )

        return Outcome(passed, [reason])

    def _between(self, condition: BetweenCondition, fund_key: str | None) -> Outcome:
        left = self._operand_value(condition.left, fund_key)
        if left is None:
            return self.short_history(condition.left.indicator)

        passed = condition.low <= left <= condition.high
        format_name = _indicator_of(condition.left)
        reason = msg(
            "rule_based.reason.between",
            left=self._operand_label(condition.left, left, format_name),
            leftValue=self.shown(format_name, left) if format_name else left,
            low=self.shown(format_name, condition.low) if format_name else condition.low,
            high=self.shown(format_name, condition.high) if format_name else condition.high,
            verdict=_verdict(passed),
        )

        return Outcome(passed, [reason])

    def _crossed(self, condition: CrossedCondition, fund_key: str | None) -> Outcome:
        series = self.series_of(condition.indicator, fund_key).dropna()
        if len(series) <= condition.within_days:
            return self.short_history(condition.indicator, condition.within_days)

        before, now = float(series.iloc[-1 - condition.within_days]), float(series.iloc[-1])
        level_before, level_now = self._crossing_levels(condition, series, fund_key)
        if level_before is None or level_now is None:
            return self.short_history(condition.other or condition.indicator, condition.within_days)

        passed = _has_crossed(condition.direction, before, now, level_before, level_now)
        target = condition.other if condition.other is not None else self.shown(condition.indicator, condition.level)
        reason = msg(
            "rule_based.reason.crossed",
            indicator=condition.indicator,
            direction=msg(f"rule_based.choice.direction.{condition.direction}"),
            target=target,
            withinDays=condition.within_days,
            verdict=_verdict(passed),
        )

        return Outcome(passed, [reason])

    def _crossing_levels(self, condition: CrossedCondition, series: pd.Series, fund_key: str | None) -> tuple[float | None, float | None]:
        if condition.other is None:
            return condition.level, condition.level

        other = self.series_of(condition.other, fund_key).reindex(series.index)

        return _known(other.iloc[-1 - condition.within_days]), _known(other.iloc[-1])

    def _signal(self, condition: SignalCondition, fund_key: str | None) -> Outcome:
        state = self._signals.get(condition.signal_id)
        if state is None:
            raise ContextError(f"signal {condition.signal_id} has no state in the context")

        active = fund_key in state.active_for if condition.scope == "thisFund" else bool(state.active_for)
        passed = active if condition.state == "active" else not active
        reason = msg(
            "rule_based.reason.signal",
            state=msg(f"rule_based.choice.state.{'active' if active else 'inactive'}"),
            scope=msg(f"rule_based.choice.scope.{condition.scope}"),
            verdict=_verdict(passed),
        )

        return Outcome(passed, [reason])

    def _all(self, condition: AllCondition, fund_key: str | None) -> Outcome:
        outcomes = [self.read(child, fund_key) for child in condition.conditions]
        passed = all(outcome.passed for outcome in outcomes)
        telling = outcomes if passed else [outcome for outcome in outcomes if not outcome.passed]

        return Outcome(passed, _reasons_of(telling))

    def _any(self, condition: AnyCondition, fund_key: str | None) -> Outcome:
        outcomes = [self.read(child, fund_key) for child in condition.conditions]
        passed = any(outcome.passed for outcome in outcomes)
        telling = [outcome for outcome in outcomes if outcome.passed] if passed else outcomes

        return Outcome(passed, _reasons_of(telling))

    def _not(self, condition: NotCondition, fund_key: str | None) -> Outcome:
        inner = self.read(condition.condition, fund_key)
        reason = msg("rule_based.reason.not", items=inner.reasons, verdict=_verdict(not inner.passed))

        return Outcome(not inner.passed, [reason])

    def _operand_value(self, operand: Operand, fund_key: str | None) -> float | None:
        if isinstance(operand, IndicatorOperand):
            return self.value_of(operand.indicator, fund_key)

        return operand.value

    def _operand_label(self, operand: Operand, value: float, format_name: str | None) -> str | float:
        if isinstance(operand, IndicatorOperand):
            return operand.indicator

        return self.shown(format_name, value) if format_name else value

    @staticmethod
    def _first_missing(sides: list[tuple[Operand, float | None]]) -> str | None:
        for operand, value in sides:
            if value is None and isinstance(operand, IndicatorOperand):
                return operand.indicator

        return None


def _indicator_of(operand: Operand) -> str | None:
    return operand.indicator if isinstance(operand, IndicatorOperand) else None


def _has_crossed(direction: str, before: float, now: float, level_before: float, level_now: float) -> bool:
    if direction == "down":
        return before >= level_before and now < level_now

    return before <= level_before and now > level_now


def _known(value) -> float | None:
    number = float(value)

    return None if math.isnan(number) else number


def _verdict(passed: bool) -> str:
    return msg("rule_based.reason.holds") if passed else msg("rule_based.reason.fails")


def _reasons_of(outcomes: list[Outcome]) -> list[str]:
    return [reason for outcome in outcomes for reason in outcome.reasons]
