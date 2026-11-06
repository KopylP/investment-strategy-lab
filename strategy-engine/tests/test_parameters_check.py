from __future__ import annotations

import copy
import json
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app
from finance_decisions.strategies.rule_based.parameters import RuleParameters
from test_rule_based_parameters import MOMENTUM_RULES, SIGNAL_ID

CHECK = "/v1/strategies/rule-based/parameters/check"
RULE_SCHEMA_PATH = Path(__file__).resolve().parents[1] / "contracts" / "v1" / "rule-based.parameters.schema.json"


@pytest.fixture
def client() -> TestClient:
    return TestClient(create_app())


def _check(client: TestClient, parameters: dict, url: str = CHECK) -> dict:
    response = client.post(url, json={"parameters": parameters})
    assert response.status_code == 200, response.text
    return response.json()


def test_valid_rules_have_no_problems_and_list_their_signals(client: TestClient) -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["exit"] = {"condition": {"kind": "signal", "signalId": SIGNAL_ID, "state": "active", "scope": "thisFund"}}

    assert _check(client, rules) == {"problems": [], "signalIds": [SIGNAL_ID], "historyBars": 260}


def test_a_shape_problem_has_a_json_path_without_union_tags(client: TestClient) -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["entry"]["condition"]["conditions"][0]["right"]["value"] = "x"

    problems = _check(client, rules)["problems"]

    assert {"path": ["entry", "condition", "conditions", 0, "right", "value"], "code": "engine.parameters.float_type"} in [
        {"path": problem["path"], "code": problem["code"]} for problem in problems
    ]


def test_a_missing_field_keeps_its_name_in_the_path(client: TestClient) -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    del rules["indicators"]["trend"]["days"]

    problems = _check(client, rules)["problems"]

    assert [(problem["path"], problem["code"]) for problem in problems] == [(["indicators", "trend", "days"], "engine.parameters.missing")]


def test_a_meaning_problem_has_the_type_code(client: TestClient) -> None:
    rules = copy.deepcopy(MOMENTUM_RULES)
    rules["selection"]["rankBy"] = "nope"

    problems = _check(client, rules)["problems"]

    assert [problem["code"] for problem in problems] == ["rule_based.check.rank_by_unknown"]
    assert problems[0]["message"].startswith("@rule_based.check.rank_by_unknown")


def test_every_type_can_be_checked(client: TestClient) -> None:
    problems = _check(client, {"targetVolatility": 5}, "/v1/strategies/adaptive-momentum/parameters/check")["problems"]

    assert [(problem["path"], problem["code"]) for problem in problems] == [(["targetVolatility"], "engine.parameters.less_than_equal")]
    assert problems[0]["message"] == '@engine.parameters.less_than_equal {"le": 1.0}'


def test_an_unknown_type_is_404(client: TestClient) -> None:
    assert client.post("/v1/strategies/nope/parameters/check", json={"parameters": {}}).status_code == 404


def test_the_exported_rule_schema_is_fresh() -> None:
    expected = json.dumps(RuleParameters.model_json_schema(by_alias=True), indent=2, sort_keys=True, ensure_ascii=False) + "\n"

    assert RULE_SCHEMA_PATH.read_text(encoding="utf-8") == expected


def test_every_type_sends_the_texts_of_parameter_problems(client: TestClient) -> None:
    types = client.get("/v1/strategies").json()

    assert all("engine.parameters.float_type" in item["messages"] for item in types)
