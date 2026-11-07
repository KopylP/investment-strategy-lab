from __future__ import annotations

import json
import math
import time
from datetime import date, timedelta
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app

RULES = Path(__file__).resolve().parents[2] / "backend/tests/Modules/InvestmentLab.StrategyBuilder.Tests/Fixtures/GeneratedRules"
TRADING_DAYS = 531
FUNDS = 10
REPEATS = 30


def _trading_days() -> list[date]:
    days: list[date] = []
    day = date(2022, 1, 3)
    while len(days) < TRADING_DAYS:
        if day.weekday() < 5:
            days.append(day)
        day += timedelta(days=1)
    return days


def _series(key: str, role: str, phase: float, days: list[date]) -> dict:
    closes = [100 * math.exp(0.0003 * index + 0.05 * math.sin(index / 20 + phase)) for index in range(len(days))]
    return {"key": key, "role": role, "dates": [day.isoformat() for day in days], "adjustedClose": closes, "close": closes}


def _payload(rules: str) -> str:
    days = _trading_days()
    funds = [f"fund{index}" for index in range(FUNDS)]
    series = [_series(key, "instrument", index, days) for index, key in enumerate(funds)]
    series += [_series("benchmark", "benchmark", 0.3, days), _series("cash", "instrument", 0.0, days)]
    body = {
        "asOf": days[-1].isoformat(),
        "parameters": json.loads((RULES / rules).read_text()),
        "universe": [{"key": key, "name": key} for key in funds],
        "cashInstrumentKey": "cash",
        "marketSeries": series,
        "subPortfolio": {"currency": "EUR", "cash": 1000, "holdings": [{"key": "fund1", "lots": [{"quantity": 10, "price": 100}]}]},
    }
    return json.dumps(body)


@pytest.mark.slow
@pytest.mark.parametrize("rules", ["momentum-trend-filter.json", "every-indicator.json"])
@pytest.mark.parametrize("route", ["decide", "risk"])
def test_a_realistic_call_stays_fast(rules: str, route: str) -> None:
    client = TestClient(create_app())
    body = _payload(rules)
    headers = {"content-type": "application/json"}
    assert client.post(f"/v1/strategies/rule-based/{route}", content=body, headers=headers).status_code == 200

    started = time.perf_counter()
    for _ in range(REPEATS):
        client.post(f"/v1/strategies/rule-based/{route}", content=body, headers=headers)
    milliseconds = (time.perf_counter() - started) / REPEATS * 1000

    print(f"{route} {rules}: {milliseconds:.1f} ms per call")
    assert milliseconds < 500
