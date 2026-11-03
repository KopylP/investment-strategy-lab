from __future__ import annotations

import pandas as pd
import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app
from test_api_contracts import _context, _frame

ADAPTIVE = "/v1/strategies/adaptive-momentum/decide"
RULES = "/v1/strategies/rule-based/decide"

ADAPTIVE_ENTRY = {
    "kind": "all",
    "conditions": [
        {"kind": "compare", "left": {"indicator": "mom"}, "operator": "gt", "right": {"value": 0}},
        {"kind": "compare", "left": {"indicator": "trend"}, "operator": "gt", "right": {"value": 0}},
    ],
}


def _adaptive_as_rules(target_volatility: float) -> dict:
    return {
        "formatVersion": 1,
        "indicators": {
            "mom": {"kind": "blendedMomentum", "of": {"series": "fund"}},
            "trend": {"kind": "priceVsSma", "of": {"series": "fund"}, "days": 200},
        },
        "entry": {"condition": ADAPTIVE_ENTRY},
        "exit": {"condition": {"kind": "not", "condition": ADAPTIVE_ENTRY}},
        "selection": {"rankBy": "mom", "order": "desc", "top": 3, "maxPerGroup": 1, "maxCorrelation": 0.7, "correlationDays": 126},
        "weights": {"method": "equal", "targetVolatility": target_volatility, "targetVolatilityDays": 21},
    }


@pytest.fixture
def client() -> TestClient:
    return TestClient(create_app())


def _both(client: TestClient, frame: pd.DataFrame, holdings: list[dict], target_volatility: float) -> tuple[dict, dict]:
    adaptive = client.post(ADAPTIVE, json=_context(frame, holdings=holdings, parameters={"targetVolatility": target_volatility}))
    rules = client.post(RULES, json=_context(frame, holdings=holdings, parameters=_adaptive_as_rules(target_volatility)))
    assert adaptive.status_code == 200, adaptive.text
    assert rules.status_code == 200, rules.text

    return adaptive.json(), rules.json()


def _weights(decision: dict) -> dict[str, float]:
    return {target["key"]: target["weight"] for target in decision["targets"]}


def _statuses(decision: dict) -> dict[str, str]:
    return {score["key"]: score["status"] for score in decision["rationale"]["scores"]}


def _assert_same(adaptive: dict, rules: dict) -> None:
    adaptive_weights, rule_weights = _weights(adaptive), _weights(rules)

    assert set(rule_weights) == set(adaptive_weights)
    for key, weight in adaptive_weights.items():
        assert rule_weights[key] == pytest.approx(weight, rel=1e-9)
    assert _statuses(rules) == _statuses(adaptive)


@pytest.mark.parametrize("holdings", [[], [{"key": "DDD", "lots": [{"quantity": 1, "price": 100}]}]])
@pytest.mark.parametrize("target_volatility", [1.0, 0.14, 0.02])
def test_rules_like_the_adaptive_strategy_pick_the_same_funds_with_the_same_weights(client: TestClient, holdings: list[dict], target_volatility: float) -> None:
    adaptive, rules = _both(client, _frame(), holdings, target_volatility)

    _assert_same(adaptive, rules)


def test_correlated_funds_are_skipped_the_same_way(client: TestClient) -> None:
    frame = _frame()
    frame["DDD"] = frame["CCC"] * 1.001

    adaptive, rules = _both(client, frame, [], 1.0)

    _assert_same(adaptive, rules)
    assert "correlated" in _statuses(rules).values()
