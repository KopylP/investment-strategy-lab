from __future__ import annotations

import copy

import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app
from test_api_contracts import _context, _frame
from test_rule_based_parameters import MOMENTUM_RULES

DECIDE = "/v1/strategies/rule-based/decide"
RISK = "/v1/strategies/rule-based/risk"


@pytest.fixture
def client() -> TestClient:
    return TestClient(create_app())


def _decide(client: TestClient, context: dict) -> dict:
    response = client.post(DECIDE, json=context)
    assert response.status_code == 200, response.text
    return response.json()


def _selected(decision: dict) -> set[str]:
    return {score["key"] for score in decision["rationale"]["scores"] if score["status"] == "selected"}


def test_the_catalog_lists_rule_based_without_defaults(client: TestClient) -> None:
    types = {item["code"]: item for item in client.get("/v1/strategies").json()}

    rule_based = types["rule-based"]
    assert rule_based["defaultParameters"] is None
    assert [report["code"] for report in rule_based["reports"]] == ["rebalance", "risk-review"]
    assert types["adaptive-momentum"]["defaultParameters"] is not None


def test_a_favourable_market_invests_everything(client: TestClient) -> None:
    decision = _decide(client, _context(_frame(), parameters=MOMENTUM_RULES))

    assert decision["investFraction"] == 1.0
    assert decision["market"]["isFavorable"] is True


def test_an_unfavourable_market_invests_the_chosen_share(client: TestClient) -> None:
    decision = _decide(client, _context(_frame(spy_drift=-0.6), parameters=MOMENTUM_RULES))

    assert decision["investFraction"] == 0.3
    assert decision["market"]["isFavorable"] is False


def _rules_with_market_mode(when_unfavorable: str | None, share: float, target_volatility: float | None = None) -> dict:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["marketFilter"]["investWhenUnfavorable"] = share
    if when_unfavorable is not None:
        rules["marketFilter"]["whenUnfavorable"] = when_unfavorable
    if target_volatility is not None:
        rules["weights"]["targetVolatility"] = target_volatility
    return rules


def _unfavourable_decision(client: TestClient, when_unfavorable: str | None, share: float, target_volatility: float | None = None) -> dict:
    rules = _rules_with_market_mode(when_unfavorable, share, target_volatility)
    return _decide(client, _context(_frame(spy_drift=-0.6), parameters=rules))


def _target_weight_sum(decision: dict) -> float:
    return sum(target["weight"] for target in decision["targets"])


def _full_weight_sum(client: TestClient, target_volatility: float | None = None) -> float:
    rules = _rules_with_market_mode(None, 1.0, target_volatility)
    return _target_weight_sum(_decide(client, _context(_frame(spy_drift=-0.6), parameters=rules)))


def test_reduce_holdings_scales_targets_to_the_share(client: TestClient) -> None:
    decision = _unfavourable_decision(client, "reduceHoldings", share=0.2)

    assert _target_weight_sum(decision) == pytest.approx(0.2 * _full_weight_sum(client))
    assert decision["investFraction"] == 1.0
    assert decision["market"]["isFavorable"] is False


def test_new_money_only_keeps_targets_and_splits_contributions(client: TestClient) -> None:
    decision = _unfavourable_decision(client, "newMoneyOnly", share=0.2)

    assert _target_weight_sum(decision) == pytest.approx(_full_weight_sum(client))
    assert decision["investFraction"] == 0.2


def test_a_filter_without_a_mode_keeps_new_money_only(client: TestClient) -> None:
    decision = _unfavourable_decision(client, None, share=0.2)

    assert decision["investFraction"] == 0.2
    assert _target_weight_sum(decision) == pytest.approx(_full_weight_sum(client))


def test_reduce_holdings_with_target_volatility_scales_once(client: TestClient) -> None:
    decision = _unfavourable_decision(client, "reduceHoldings", share=0.5, target_volatility=0.02)

    full = _full_weight_sum(client, target_volatility=0.02)
    assert full < 1.0
    assert _target_weight_sum(decision) == pytest.approx(0.5 * full)


def test_a_favourable_market_ignores_the_mode(client: TestClient) -> None:
    rules = _rules_with_market_mode("reduceHoldings", 0.2)
    decision = _decide(client, _context(_frame(), parameters=rules))

    assert decision["investFraction"] == 1.0
    assert _target_weight_sum(decision) == pytest.approx(1.0)


def test_targets_are_the_selected_funds_with_their_weights(client: TestClient) -> None:
    decision = _decide(client, _context(_frame(), parameters=MOMENTUM_RULES))

    targets = {target["key"]: target["weight"] for target in decision["targets"]}
    assert set(targets) == _selected(decision)
    assert len(targets) == 3
    assert all(weight == pytest.approx(1 / 3) for weight in targets.values())


def test_the_rationale_shows_the_indicators_the_rules_use(client: TestClient) -> None:
    decision = _decide(client, _context(_frame(), parameters=MOMENTUM_RULES))

    assert [column["id"] for column in decision["rationale"]["columns"]] == ["mom", "trend"]
    assert set(decision["rationale"]["scores"][0]["values"]) == {"mom", "trend"}


def test_a_market_filter_without_its_series_refuses_the_context(client: TestClient) -> None:
    context = _context(_frame(), parameters=MOMENTUM_RULES)
    context["marketSeries"] = [series for series in context["marketSeries"] if series["role"] != "benchmark"]

    response = client.post(DECIDE, json=context)

    assert response.status_code == 422
    assert "benchmark" in response.json()["detail"]


def test_rules_with_a_meaning_problem_are_refused_with_its_path(client: TestClient) -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["entry"]["condition"]["conditions"][0]["left"] = {"indicator": "nope"}

    response = client.post(DECIDE, json=_context(_frame(), parameters=rules))

    assert response.status_code == 422
    assert response.json()["detail"][0]["path"][-1] == "indicator"


def test_the_stop_loss_sells_a_position_below_its_cost(client: TestClient) -> None:
    frame = _frame()
    rules = {**MOMENTUM_RULES, "risk": {"stopLossFromCost": -0.12}}
    holdings = [{"key": "AAA", "lots": [{"quantity": 1, "price": 2 * float(frame["AAA"].iloc[-1])}]}]

    response = client.post(RISK, json=_context(frame, holdings=holdings, parameters=rules))

    assessment = response.json()
    assert response.status_code == 200, response.text
    assert assessment["status"] == "act"
    assert [(signal["key"], signal["kind"]) for signal in assessment["signals"]] == [("AAA", "exit")]


def test_an_exit_rule_that_fires_between_rebalances_sells(client: TestClient) -> None:
    rules = {**MOMENTUM_RULES, "exit": {"condition": {"kind": "compare", "left": {"indicator": "mom"}, "operator": "lt", "right": {"value": 0}}}}
    holdings = [{"key": "FFF", "lots": [{"quantity": 1, "price": 1}]}, {"key": "AAA", "lots": [{"quantity": 1, "price": 1}]}]

    assessment = client.post(RISK, json=_context(_frame(), holdings=holdings, parameters=rules)).json()

    assert [(signal["key"], signal["kind"]) for signal in assessment["signals"]] == [("FFF", "exit")]


def test_a_quiet_portfolio_needs_no_action(client: TestClient) -> None:
    holdings = [{"key": "AAA", "lots": [{"quantity": 1, "price": 1}]}]

    assessment = client.post(RISK, json=_context(_frame(), holdings=holdings, parameters=MOMENTUM_RULES)).json()

    assert assessment["status"] == "ok"
    assert assessment["signals"] == []
