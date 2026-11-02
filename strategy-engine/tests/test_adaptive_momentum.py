from __future__ import annotations

import numpy as np
import pandas as pd
import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app
from finance_decisions.strategies.adaptive_momentum.exposure import exposure_for
from test_api_contracts import AS_OF, _context, _dates, _frame, _series

DECIDE = "/v1/strategies/adaptive-momentum/decide"


@pytest.fixture
def client() -> TestClient:
    return TestClient(create_app())


def _decide(client: TestClient, frame: pd.DataFrame, parameters: dict | None = None) -> dict:
    response = client.post(DECIDE, json=_context(frame, parameters=parameters))
    assert response.status_code == 200, response.text

    return response.json()


def _weights(decision: dict) -> dict[str, float]:
    return {target["key"]: target["weight"] for target in decision["targets"]}


def _statuses(decision: dict) -> dict[str, str]:
    return {score["key"]: score["status"] for score in decision["rationale"]["scores"]}


def test_holds_the_three_strongest_rising_funds_one_per_group(client: TestClient) -> None:
    decision = _decide(client, _frame(), {"targetVolatility": 1.0})

    statuses = _statuses(decision)

    assert set(_weights(decision)) == {"CCC", "EEE", "BBB"}
    assert statuses["AAA"] == "group_blocked"
    assert statuses["FFF"] == "failed"
    assert statuses["DDD"] == "reserve"


def test_each_pick_takes_one_slot_and_an_empty_slot_stays_in_cash(client: TestClient) -> None:
    decision = _decide(client, _frame(), {"targetVolatility": 1.0, "scanner": {"topN": 8}})

    weights = _weights(decision)

    assert all(weight == pytest.approx(1 / 8) for weight in weights.values())
    assert sum(weights.values()) < 1.0


def test_a_portfolio_more_volatile_than_the_target_keeps_part_in_cash(client: TestClient) -> None:
    calm = _weights(_decide(client, _frame(), {"targetVolatility": 1.0}))
    restrained = _decide(client, _frame(), {"targetVolatility": 0.02})

    weights = _weights(restrained)

    assert set(weights) == set(calm)
    assert sum(weights.values()) < sum(calm.values())
    assert restrained["market"]["isFavorable"] is True


def test_exposure_is_the_target_divided_by_the_measured_volatility() -> None:
    index = _dates()
    swings = pd.DataFrame({"AAA": _series(0.2, 1, noise=0.03), "BBB": _series(0.2, 2, noise=0.03)}, index=index)

    exposure = exposure_for(swings, {"AAA": 0.5, "BBB": 0.5}, target=0.10, window_days=21)

    assert exposure.volatility > 0.10
    assert exposure.share == pytest.approx(0.10 / exposure.volatility)


def test_a_fund_that_moves_like_one_already_chosen_is_skipped(client: TestClient) -> None:
    frame = _frame()
    frame["DDD"] = frame["CCC"] * 1.001

    decision = _decide(client, frame, {"targetVolatility": 1.0, "scanner": {"maxCorrelation": 0.7}})

    statuses = _statuses(decision)

    assert sorted([statuses["CCC"], statuses["DDD"]]) == ["correlated", "selected"]


def test_prices_after_the_report_date_are_ignored(client: TestClient) -> None:
    frame = _frame()
    future = pd.bdate_range(start=pd.Timestamp(AS_OF) + pd.Timedelta(days=1), periods=30)
    crash = pd.DataFrame({column: frame[column].iloc[-1] * np.linspace(1, 0.3, len(future)) for column in frame.columns}, index=future)

    without_future = _decide(client, frame, {"targetVolatility": 1.0})
    with_future = _decide(client, pd.concat([frame, crash]), {"targetVolatility": 1.0})

    assert with_future["asOf"] == AS_OF.isoformat()
    assert _weights(with_future) == _weights(without_future)


def test_declares_the_monthly_rebalance_and_no_stop_loss_review(client: TestClient) -> None:
    types = client.get("/v1/strategies").json()

    [descriptor] = [item for item in types if item["code"] == "adaptive-momentum"]

    assert [report["code"] for report in descriptor["reports"]] == ["rebalance"]
    assert descriptor["defaultParameters"] == {
        "scanner": {"topN": 3, "trendDays": 200, "maxCorrelation": 0.7, "maxPerGroup": 1},
        "targetVolatility": 0.14,
        "volatilityWindowDays": 21,
    }
