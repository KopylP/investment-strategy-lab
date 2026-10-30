"""Background jobs of /decide and /risk: the same answers, delivered later."""

from __future__ import annotations

import threading
import time

import pytest
from fastapi.testclient import TestClient

from finance_decisions.api import jobs
from finance_decisions.api.main import create_app
from test_api_contracts import _context, _frame


@pytest.fixture
def client(monkeypatch: pytest.MonkeyPatch) -> TestClient:
    monkeypatch.setattr(jobs, "store", jobs.JobStore(workers=2))
    return TestClient(create_app())


def _wait(client: TestClient, job_id: str, timeout: float = 10.0) -> dict:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        job = client.get(f"/v1/jobs/{job_id}").json()
        if job["status"] != "running":
            return job
        time.sleep(0.02)
    raise AssertionError(f"job {job_id} still runs")


def test_decision_job_answers_what_decide_answers(client: TestClient) -> None:
    context = _context(_frame())

    accepted = client.post("/v1/strategies/etf-momentum/decide/jobs", json=context)

    assert accepted.status_code == 202, accepted.text
    assert accepted.json()["status"] in ("running", "succeeded")
    job = _wait(client, accepted.json()["jobId"])
    assert job["status"] == "succeeded"
    assert (job["code"], job["action"]) == ("etf-momentum", "decide")
    assert job["result"] == client.post("/v1/strategies/etf-momentum/decide", json=context).json()


def test_risk_job_answers_what_risk_answers(client: TestClient) -> None:
    holdings = [{"key": "AAA", "lots": [{"quantity": 2, "price": 100.0}]}]
    context = _context(_frame(), holdings=holdings)

    job = _wait(client, client.post("/v1/strategies/etf-momentum/risk/jobs", json=context).json()["jobId"])

    assert job["status"] == "succeeded"
    assert job["result"] == client.post("/v1/strategies/etf-momentum/risk", json=context).json()


def test_unknown_strategy_and_bad_parameters_are_refused_at_once(client: TestClient) -> None:
    assert client.post("/v1/strategies/nope/decide/jobs", json=_context(_frame())).status_code == 404
    bad = _context(_frame(), parameters={"scanner": {"topN": 0}})
    assert client.post("/v1/strategies/etf-momentum/decide/jobs", json=bad).status_code == 422


def test_context_the_strategy_refuses_fails_the_job_with_422(client: TestClient) -> None:
    context = _context(_frame())
    context["marketSeries"] = [series for series in context["marketSeries"] if series["role"] != "benchmark"]

    job = _wait(client, client.post("/v1/strategies/etf-momentum/decide/jobs", json=context).json()["jobId"])

    assert job["status"] == "failed"
    assert job["error"]["statusCode"] == 422
    assert job["result"] is None


def test_job_runs_in_the_background_until_the_strategy_answers(client: TestClient, monkeypatch: pytest.MonkeyPatch) -> None:
    release = threading.Event()
    strategy_type = jobs.find_strategy_type("etf-momentum")
    slow = type(strategy_type)(**{**strategy_type.__dict__, "decide": lambda context, parameters: release.wait(5) and strategy_type.decide(context, parameters)})
    monkeypatch.setattr(jobs, "find_strategy_type", lambda code: slow)

    job_id = client.post("/v1/strategies/etf-momentum/decide/jobs", json=_context(_frame())).json()["jobId"]

    assert client.get(f"/v1/jobs/{job_id}").json()["status"] == "running"
    release.set()
    assert _wait(client, job_id)["status"] == "succeeded"


def test_unknown_job_is_not_found(client: TestClient) -> None:
    assert client.get("/v1/jobs/0123").status_code == 404
