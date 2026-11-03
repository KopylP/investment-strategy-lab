from __future__ import annotations

import pytest
from fastapi.testclient import TestClient

from finance_decisions import market_data
from finance_decisions.api.funds import get_fund_screener
from finance_decisions.api.main import create_app


def fund(symbol="AAA", name="MSCI World", assets=2e9, expense=0.002):
    return dict(symbol=symbol, name=name, currency="EUR", exchange=None,
                fund_family=None, total_assets=assets, expense_ratio=expense)


@pytest.fixture
def client():
    app = create_app()
    app.dependency_overrides[get_fund_screener] = lambda: lambda request: [
        fund("A", assets=1e9), fund("B", assets=None), fund("C", assets=3e9),
    ]
    return TestClient(app)


def test_the_screen_answers_with_the_funds_sorted_by_assets(client):
    response = client.post("/v1/funds/screen", json={"region": "us"})
    assert response.status_code == 200
    assert [item["symbol"] for item in response.json()["funds"]] == ["C", "A", "B"]


def test_name_words_match_regardless_of_case_or_diacritics(client):
    client.app.dependency_overrides[get_fund_screener] = lambda: lambda request: [
        fund(name="iShares Core MŚCI Wörld"), fund(name="S&P 500"),
    ]
    response = client.post("/v1/funds/screen", json={"region": "us", "nameWords": ["msci", "World"]})
    assert len(response.json()["funds"]) == 1


@pytest.mark.parametrize("criteria", [{"minAssets": 1e9}, {"maxExpenseRatio": 0.003}])
def test_a_limit_drops_funds_without_that_figure(client, criteria):
    client.app.dependency_overrides[get_fund_screener] = lambda: lambda request: [
        fund("A"), fund("B", assets=None, expense=None), fund("C", assets=1, expense=0.05),
    ]
    response = client.post("/v1/funds/screen", json={"region": "us", **criteria})
    assert [item["symbol"] for item in response.json()["funds"]] == ["A"]
    response = client.post("/v1/funds/screen", json={"region": "us"})
    assert len(response.json()["funds"]) == 3


@pytest.mark.parametrize("criteria", [{}, {"nameWords": ["world"]}, {"region": " "}])
def test_a_search_needs_an_exchange_or_a_region(client, criteria):
    assert client.post("/v1/funds/screen", json=criteria).status_code == 422


def test_the_query_names_the_providers_fields(monkeypatch):
    from finance_decisions.api.contracts import FundScreenRequest

    calls = {}

    def screen(query, **options):
        calls.update(query=query.to_dict(), **options)
        return {"quotes": [{"symbol": "AAA", "longName": "World", "netAssets": 2e9,
                            "netExpenseRatio": 0.2}]}

    monkeypatch.setattr(market_data.yf, "screen", screen)
    result = market_data.screen_funds(FundScreenRequest(exchange="GER", min_assets=1e8, max_expense_ratio=0.002))
    assert calls["query"] == {"operator": "AND", "operands": [
        {"operator": "EQ", "operands": ["exchange", "GER"]},
        {"operator": "GTE", "operands": ["fundnetassets", 1e8]},
        {"operator": "LTE", "operands": ["annualreportnetexpenseratio", 0.2]},
    ]}
    assert calls["size"] == 250
    assert calls["sortField"] == "fundnetassets"
    assert calls["sortAsc"] is False
    assert result[0]["total_assets"] == 2e9
    assert result[0]["expense_ratio"] == 0.002


def test_a_provider_failure_is_a_502(client):
    def fail(request):
        raise RuntimeError("offline")

    client.app.dependency_overrides[get_fund_screener] = lambda: fail
    response = client.post("/v1/funds/screen", json={"region": "us"})
    assert response.status_code == 502
    assert response.json()["detail"] == "fund screen failed"


def test_results_are_cut_to_the_requested_size(client):
    response = client.post("/v1/funds/screen", json={"region": "us", "size": 1})
    assert [item["symbol"] for item in response.json()["funds"]] == ["C"]


@pytest.mark.parametrize("criteria", [{"size": 251}, {"minAssets": -1}, {"maxExpenseRatio": 0.11},
                                      {"nameWords": ["a"] * 6}])
def test_invalid_limits_are_rejected(client, criteria):
    assert client.post("/v1/funds/screen", json={"region": "us", **criteria}).status_code == 422
