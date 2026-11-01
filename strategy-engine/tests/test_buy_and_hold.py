from __future__ import annotations

from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app
from test_api_contracts import _context, _frame


def test_holds_every_chosen_fund_equally_whatever_the_market_does() -> None:
    context = _context(_frame())
    context["parameters"] = {}
    members = len(context["universe"])

    decision = TestClient(create_app()).post("/v1/strategies/buy-and-hold/decide", json=context).json()

    assert decision["investFraction"] == 1.0
    assert decision["market"]["isFavorable"] is True
    assert sorted(target["key"] for target in decision["targets"]) == sorted(member["key"] for member in context["universe"])
    assert all(target["weight"] == 1.0 / members for target in decision["targets"])


def test_declares_only_the_monthly_report() -> None:
    types = TestClient(create_app()).get("/v1/strategies").json()

    [descriptor] = [item for item in types if item["code"] == "buy-and-hold"]

    assert [report["code"] for report in descriptor["reports"]] == ["rebalance"]
    assert descriptor["defaultParameters"] == {}
