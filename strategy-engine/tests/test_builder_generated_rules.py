from __future__ import annotations

import json
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app

FIXTURES = Path(__file__).resolve().parents[2] / "backend/tests/Modules/InvestmentLab.StrategyBuilder.Tests/Fixtures/GeneratedRules"


@pytest.fixture
def client() -> TestClient:
    return TestClient(create_app())


def test_the_builder_fixtures_exist() -> None:
    assert len(list(FIXTURES.glob("*.json"))) >= 7


@pytest.mark.parametrize("path", sorted(FIXTURES.glob("*.json")), ids=lambda path: path.stem)
def test_rules_the_builder_generates_pass_the_engine_check(client: TestClient, path: Path) -> None:
    response = client.post("/v1/strategies/rule-based/parameters/check", json={"parameters": json.loads(path.read_text())})

    assert response.status_code == 200
    assert response.json()["problems"] == []
