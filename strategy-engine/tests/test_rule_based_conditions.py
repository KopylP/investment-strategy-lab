from __future__ import annotations

import numpy as np
import pandas as pd
import pytest

from finance_decisions.api.contracts import EvaluationContext, SignalState
from finance_decisions.strategies.errors import ContextError
from finance_decisions.strategies.rule_based.conditions import ConditionReader
from finance_decisions.strategies.rule_based.parameters import RuleParameters
from finance_decisions.strategies.rule_based.series_book import SeriesBook

SIGNAL_ID = "4d1c2a3b-5e6f-4a7b-8c9d-0e1f2a3b4c5d"
DAYS = 300
INDICATORS = {
    "spy": {"kind": "price", "of": {"series": "benchmark"}},
    "spySma": {"kind": "sma", "of": {"series": "benchmark"}, "days": 50},
    "price": {"kind": "price", "of": {"series": "fund"}},
    "sma20": {"kind": "sma", "of": {"series": "fund"}, "days": 20},
    "long": {"kind": "sma", "of": {"series": "fund"}, "days": 280},
    "trend": {"kind": "priceVsSma", "of": {"series": "fund"}, "days": 50},
}


def _context(funds: dict[str, list[float]], benchmark: list[float], signals: list[dict] | None = None) -> EvaluationContext:
    dates = [day.date().isoformat() for day in pd.bdate_range("2024-01-01", periods=len(benchmark))]
    series = [{"key": key, "role": "instrument", "dates": dates[-len(values):], "adjustedClose": values} for key, values in funds.items()]
    series.append({"key": "SPY", "role": "benchmark", "dates": dates, "adjustedClose": benchmark})
    payload = {
        "asOf": dates[-1],
        "universe": [{"key": key} for key in funds],
        "marketSeries": series,
        "subPortfolio": {"currency": "EUR"},
    }
    if signals is not None:
        payload["signals"] = signals
    return EvaluationContext.model_validate(payload)


def _reader(context: EvaluationContext) -> ConditionReader:
    rules = RuleParameters.model_validate(
        {"formatVersion": 1, "indicators": INDICATORS, "selection": {"top": 1}, "weights": {"method": "equal"}}
    )
    return ConditionReader(rules, SeriesBook.from_context(context), context.signals)


def _rising(days: int = DAYS) -> list[float]:
    return list(100 * 1.001 ** np.arange(days))


def _condition(**condition):
    rules = RuleParameters.model_validate(
        {"formatVersion": 1, "indicators": INDICATORS, "exit": {"condition": condition}, "selection": {"top": 1}, "weights": {"method": "equal"}}
    )
    return rules.exit.condition


def _compare(left: str, operator: str, right: dict) -> dict:
    return {"kind": "compare", "left": {"indicator": left}, "operator": operator, "right": right}


def _signal(state: str = "active", scope: str = "anyInstrument") -> dict:
    return {"kind": "signal", "signalId": SIGNAL_ID, "state": state, "scope": scope}


def test_compare_reads_both_sides_at_as_of() -> None:
    reader = _reader(_context({"AAA": _rising()}, _rising()))

    outcome = reader.read(_condition(**_compare("spy", "gt", {"indicator": "spySma"})), None)

    assert outcome.passed
    assert len(outcome.reasons) == 1
    assert outcome.reasons[0].startswith("@rule_based.reason.compare ")
    assert reader.value_of("spy", None) == pytest.approx(100 * 1.001 ** (DAYS - 1))


def test_short_history_fails_the_condition_with_a_reason() -> None:
    reader = _reader(_context({"AAA": _rising(100)}, _rising()))

    outcome = reader.read(_condition(**_compare("long", "gt", {"value": 0})), "AAA")

    assert not outcome.passed
    assert outcome.reasons == ['@rule_based.reason.short_history {"name": "long", "days": 280}']


def _crossing_down(days: int = DAYS) -> list[float]:
    flat = [100.0] * (days - 10)
    return flat + [100.0 - 2 * step for step in range(1, 11)]


def test_crossed_down_needs_the_old_value_above_and_the_new_below() -> None:
    crossed = _condition(kind="crossed", indicator="trend", direction="down", level=-0.03, withinDays=21)

    assert _reader(_context({"AAA": _crossing_down()}, _rising())).read(crossed, "AAA").passed
    assert not _reader(_context({"AAA": list(reversed(_rising()))}, _rising())).read(crossed, "AAA").passed


def test_crossed_against_another_indicator() -> None:
    crossed = _condition(kind="crossed", indicator="price", direction="down", other="sma20", withinDays=21)

    assert _reader(_context({"AAA": _crossing_down()}, _rising())).read(crossed, "AAA").passed
    assert not _reader(_context({"AAA": _rising()}, _rising())).read(crossed, "AAA").passed


def test_all_any_not_nest_freely() -> None:
    reader = _reader(_context({"AAA": _rising()}, _rising()))
    true = _compare("spy", "gt", {"indicator": "spySma"})
    false = _compare("spy", "lt", {"indicator": "spySma"})

    def read(condition: dict) -> bool:
        return reader.read(_condition(**condition), "AAA").passed

    assert read({"kind": "not", "condition": {"kind": "any", "conditions": [{"kind": "all", "conditions": [true, false]}, false]}})
    assert not read({"kind": "all", "conditions": [true, {"kind": "not", "condition": true}]})
    assert read({"kind": "any", "conditions": [false, true]})


def test_a_signal_active_for_any_instrument() -> None:
    active = _reader(_context({"AAA": _rising()}, _rising(), [{"id": SIGNAL_ID, "activeFor": ["X"]}]))
    quiet = _reader(_context({"AAA": _rising()}, _rising(), [{"id": SIGNAL_ID, "activeFor": []}]))

    assert active.read(_condition(**_signal()), None).passed
    assert not quiet.read(_condition(**_signal()), None).passed


def test_a_signal_active_for_this_fund() -> None:
    reader = _reader(_context({"AAA": _rising(), "BBB": _rising()}, _rising(), [{"id": SIGNAL_ID, "activeFor": ["AAA"]}]))
    condition = _condition(**_signal(scope="thisFund"))

    assert reader.read(condition, "AAA").passed
    assert not reader.read(condition, "BBB").passed


def test_inactive_state_negates_the_signal() -> None:
    reader = _reader(_context({"AAA": _rising()}, _rising(), [{"id": SIGNAL_ID, "activeFor": []}]))

    assert reader.read(_condition(**_signal(state="inactive")), None).passed


def test_a_missing_signal_state_refuses_the_context() -> None:
    reader = _reader(_context({"AAA": _rising()}, _rising()))

    with pytest.raises(ContextError, match=SIGNAL_ID):
        reader.read(_condition(**_signal()), None)


def test_the_context_accepts_signal_states() -> None:
    context = _context({"AAA": _rising()}, _rising(), [{"id": SIGNAL_ID, "activeFor": []}])

    assert context.signals == [SignalState(id=SIGNAL_ID, active_for=[])]
