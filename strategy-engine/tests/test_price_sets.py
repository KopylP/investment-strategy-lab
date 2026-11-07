from __future__ import annotations

import json
import math
from datetime import date, timedelta
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app

RULES = Path(__file__).resolve().parents[2] / "backend/tests/Modules/InvestmentLab.StrategyBuilder.Tests/Fixtures/GeneratedRules"
EXTRA = "0199f000-0000-7000-8000-0000000000a1"
TRADING_DAYS = 420
FIRST_COMPARED_DAY = 300
EVERY_NTH_DAY = 3
WINDOW_DAYS = 400
HELD = "fund1"
VALUED = ("fund1", "fund2")


@pytest.fixture
def client() -> TestClient:
    return TestClient(create_app())


def _trading_days() -> list[date]:
    days: list[date] = []
    day = date(2023, 1, 2)
    while len(days) < TRADING_DAYS:
        if day.weekday() < 5:
            days.append(day)
        day += timedelta(days=1)
    return days


DAYS = _trading_days()


def _series(key: str, role: str, phase: float, valued: bool) -> dict:
    closes = [100 * math.exp(0.0004 * index + 0.06 * math.sin(index / 17 + phase)) for index in range(TRADING_DAYS)]
    series = {"key": key, "role": role, "dates": [day.isoformat() for day in DAYS], "adjustedClose": closes, "close": closes}
    if valued:
        series["valuationClose"] = [close * 4.2 for close in closes]
    return series


def _all_series(with_unheld_valuations: bool) -> list[dict]:
    funds = [_series(f"fund{index}", "instrument", index, valued=f"fund{index}" == HELD or (with_unheld_valuations and f"fund{index}" in VALUED))
             for index in range(6)]
    return funds + [_series("bench", "benchmark", 0.4, False), _series("cash", "instrument", 0.0, False), _series(EXTRA, "extra", 1.3, False)]


def _cut(series: dict, first: date, last: date) -> dict:
    kept = [index for index, day in enumerate(DAYS) if first <= day <= last]
    cut = {**series, "dates": [series["dates"][index] for index in kept]}
    for field in ("adjustedClose", "close", "valuationClose"):
        if field in series:
            cut[field] = [series[field][index] for index in kept]
    return cut


def _context(rules: str, as_of: date) -> dict:
    return {
        "asOf": as_of.isoformat(),
        "parameters": json.loads((RULES / rules).read_text()),
        "universe": [{"key": f"fund{index}", "name": f"fund{index}"} for index in range(6)],
        "cashInstrumentKey": "cash",
        "subPortfolio": {"currency": "PLN", "cash": 500, "holdings": [{"key": HELD, "lots": [{"quantity": 10, "price": 400}]}]},
    }


@pytest.mark.parametrize("rules", ["every-indicator.json", "extra-series.json", "risk-only-limits.json", "momentum-trend-filter.json"])
@pytest.mark.parametrize("route", ["decide", "risk"])
def test_a_call_with_a_price_set_answers_exactly_as_the_full_call_cut_at_its_window(client: TestClient, rules: str, route: str) -> None:
    uploaded = client.post("/v1/strategies/rule-based/price-sets", json={"marketSeries": _all_series(with_unheld_valuations=True)})
    assert uploaded.status_code == 201
    set_id = uploaded.json()["id"]
    full_series = _all_series(with_unheld_valuations=False)

    for as_of in DAYS[FIRST_COMPARED_DAY::EVERY_NTH_DAY]:
        first = as_of - timedelta(days=WINDOW_DAYS)
        full = {**_context(rules, as_of), "marketSeries": [_cut(series, first, as_of) for series in full_series]}
        named = {**_context(rules, as_of), "priceSetId": set_id, "historyFrom": first.isoformat()}

        full_answer = client.post(f"/v1/strategies/rule-based/{route}", json=full)
        named_answer = client.post(f"/v1/strategies/rule-based/{route}", json=named)

        assert named_answer.status_code == full_answer.status_code == 200, named_answer.text
        assert named_answer.json() == full_answer.json(), as_of


def test_an_unknown_price_set_is_answered_with_its_code(client: TestClient) -> None:
    context = {**_context("momentum-trend-filter.json", DAYS[-1]), "priceSetId": "missing", "historyFrom": DAYS[0].isoformat()}

    response = client.post("/v1/strategies/rule-based/decide", json=context)

    assert response.status_code == 404
    assert response.json()["detail"] == "price_set.unknown"


def test_a_call_cannot_carry_both_series_and_a_price_set(client: TestClient) -> None:
    context = {**_context("momentum-trend-filter.json", DAYS[-1]), "priceSetId": "any", "marketSeries": _all_series(False)}

    response = client.post("/v1/strategies/rule-based/decide", json=context)

    assert response.status_code == 422


def test_a_set_unused_for_too_long_is_forgotten() -> None:
    from finance_decisions.api.price_sets import PriceSetStore

    store = PriceSetStore(kept_for_seconds=0)
    set_id = store.add([])

    assert store.find(set_id) is None
