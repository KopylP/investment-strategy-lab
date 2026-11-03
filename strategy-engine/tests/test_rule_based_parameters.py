from __future__ import annotations

import copy
import json
import re

import pytest
from pydantic import ValidationError

from finance_decisions.messages import _all_messages
from finance_decisions.strategies.rule_based.checks import RuleProblem, problems_in, referenced_signals
from finance_decisions.strategies.rule_based.parameters import RuleParameters

SIGNAL_ID = "4d1c2a3b-5e6f-4a7b-8c9d-0e1f2a3b4c5d"
OTHER_SIGNAL_ID = "9f8e7d6c-5b4a-4392-8170-6f5e4d3c2b1a"

MOMENTUM_RULES = {
    "formatVersion": 1,
    "indicators": {
        "mom": {"kind": "blendedMomentum", "of": {"series": "fund"}},
        "trend": {"kind": "priceVsSma", "of": {"series": "fund"}, "days": 200},
        "spy": {"kind": "price", "of": {"series": "benchmark"}},
        "spySma": {"kind": "sma", "of": {"series": "benchmark"}, "days": 200},
    },
    "marketFilter": {
        "condition": {"kind": "compare", "left": {"indicator": "spy"}, "operator": "gt", "right": {"indicator": "spySma"}},
        "investWhenUnfavorable": 0.3,
    },
    "entry": {
        "condition": {
            "kind": "all",
            "conditions": [
                {"kind": "compare", "left": {"indicator": "mom"}, "operator": "gt", "right": {"value": 0}},
                {"kind": "compare", "left": {"indicator": "trend"}, "operator": "gt", "right": {"value": 0}},
            ],
        }
    },
    "selection": {"rankBy": "mom", "top": 3, "maxPerGroup": 1, "maxCorrelation": 0.7},
    "weights": {"method": "equal"},
}


def _rules(**changes) -> dict:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules.update(changes)
    return rules


def _problems(rules: dict) -> list[RuleProblem]:
    return problems_in(RuleParameters.model_validate(rules))


def _signal(signal_id: str, scope: str = "anyInstrument") -> dict:
    return {"kind": "signal", "signalId": signal_id, "state": "active", "scope": scope}


def _error_types(rules: dict) -> set[str]:
    with pytest.raises(ValidationError) as raised:
        RuleParameters.model_validate(rules)
    return {error["type"] for error in raised.value.errors()}


def test_the_momentum_rules_are_valid() -> None:
    assert _problems(MOMENTUM_RULES) == []


def test_an_unknown_field_is_refused() -> None:
    rules = _rules(entry={"condition": MOMENTUM_RULES["entry"]["condition"], "extra": 1})

    assert "extra_forbidden" in _error_types(rules)


def test_an_unknown_indicator_kind_is_refused() -> None:
    rules = _rules(indicators={"mom": {"kind": "macd", "of": {"series": "fund"}}})

    assert "union_tag_invalid" in _error_types(rules)


def test_a_reference_to_a_missing_indicator_is_a_problem_at_its_path() -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["entry"]["condition"]["conditions"][0]["left"] = {"indicator": "nope"}

    assert _problems(rules) == [
        RuleProblem(("entry", "condition", "conditions", 0, "left", "indicator"), "rule_based.check.indicator_unknown", {"name": "nope"})
    ]


def test_the_fund_series_is_not_allowed_in_the_market_filter() -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["marketFilter"]["condition"]["left"] = {"indicator": "mom"}

    assert _problems(rules) == [
        RuleProblem(("marketFilter", "condition", "left", "indicator"), "rule_based.check.fund_in_market_filter", {"name": "mom"})
    ]


def test_a_this_fund_signal_is_not_allowed_in_the_market_filter() -> None:
    rules = _rules(marketFilter={"condition": _signal(SIGNAL_ID, scope="thisFund"), "investWhenUnfavorable": 0})

    assert [problem.code for problem in _problems(rules)] == ["rule_based.check.this_fund_signal_in_market_filter"]


def test_crossed_needs_exactly_one_side() -> None:
    both = {"kind": "crossed", "indicator": "trend", "direction": "down", "level": -0.03, "other": "mom"}
    neither = {"kind": "crossed", "indicator": "trend", "direction": "down"}

    for condition in (both, neither):
        problems = _problems(_rules(exit={"condition": condition}))
        assert problems == [RuleProblem(("exit", "condition"), "rule_based.check.crossed_needs_one_side", {})]


def test_an_extra_series_needs_its_instrument() -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["indicators"]["gold"] = {"kind": "price", "of": {"series": "extra"}}

    assert _problems(rules) == [
        RuleProblem(("indicators", "gold", "of", "instrumentId"), "rule_based.check.extra_needs_instrument", {"name": "gold"})
    ]


def test_only_an_extra_series_names_an_instrument() -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["indicators"]["spy"]["of"]["instrumentId"] = SIGNAL_ID

    assert [problem.code for problem in _problems(rules)] == ["rule_based.check.instrument_without_extra"]


def test_a_signal_id_must_be_a_guid() -> None:
    rules = _rules(exit={"condition": _signal("abc")})

    assert _problems(rules) == [RuleProblem(("exit", "condition", "signalId"), "rule_based.check.signal_id_format", {"id": "abc"})]


def test_rank_by_must_name_an_indicator() -> None:
    rules = _rules(selection={"rankBy": "nope", "top": 3})

    assert _problems(rules) == [RuleProblem(("selection", "rankBy"), "rule_based.check.rank_by_unknown", {"name": "nope"})]


def test_signals_are_listed_once_in_the_order_they_appear() -> None:
    exit_rule = {"kind": "any", "conditions": [_signal(SIGNAL_ID), _signal(OTHER_SIGNAL_ID), _signal(SIGNAL_ID, "thisFund")]}
    rules = _rules(exit={"condition": exit_rule})

    assert referenced_signals(RuleParameters.model_validate(rules)) == [SIGNAL_ID, OTHER_SIGNAL_ID]


def test_only_format_version_1_is_known() -> None:
    assert "literal_error" in _error_types(_rules(formatVersion=2))


def test_an_indicator_name_is_a_short_identifier() -> None:
    rules = _rules(indicators={"Bad name": {"kind": "price", "of": {"series": "fund"}}})

    assert "string_pattern_mismatch" in _error_types(rules)


def test_every_text_of_the_schema_is_in_the_catalog() -> None:
    schema = json.dumps(RuleParameters.model_json_schema(by_alias=True))
    codes = set(re.findall(r'"@([a-z_][a-zA-Z0-9_.]*)', schema))

    assert codes
    assert sorted(codes - _all_messages().keys()) == []
