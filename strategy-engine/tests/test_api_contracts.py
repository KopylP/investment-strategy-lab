"""Contract tests for the strategy engine HTTP API (published language with .NET)."""

from __future__ import annotations

import json
from datetime import date
from pathlib import Path

import numpy as np
import pandas as pd
import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app
from finance_decisions.api.prices import get_quote_downloader
from finance_decisions.models import Position
from finance_decisions.strategies.etf_momentum.config import RiskSettings, ScannerSettings
from finance_decisions.strategies.etf_momentum.risk import analyze_portfolio_risk
from finance_decisions.strategies.etf_momentum.scanner import scan_etfs

AS_OF = date(2026, 10, 2)
CONTRACT_PATH = Path(__file__).resolve().parents[1] / "contracts" / "v1" / "openapi.json"


@pytest.fixture
def client() -> TestClient:
    return TestClient(create_app())


def _dates(periods: int = 420) -> pd.DatetimeIndex:
    return pd.bdate_range(end=pd.Timestamp(AS_OF), periods=periods)


def _series(drift: float, seed: int, periods: int = 420, start: float = 100.0, noise: float = 0.006) -> list[float]:
    """Deterministic trending price path with daily noise."""
    rng = np.random.default_rng(seed)
    returns = drift / 252 + rng.normal(0, noise, periods)
    return list(start * np.exp(np.cumsum(returns)))


UNIVERSE = {
    "AAA": (0.40, 1, "Group A"),
    "BBB": (0.30, 2, "Group B"),
    "CCC": (0.25, 3, "Group C"),
    "DDD": (0.20, 4, "Group D"),
    "EEE": (0.35, 5, "Group A"),  # same group as AAA, so it competes with AAA
    "FFF": (-0.30, 6, "Group F"),  # falling, fails the filters
}


def _frame(spy_drift: float = 0.15, vix: float = 18.0) -> pd.DataFrame:
    index = _dates()
    columns = {key: _series(drift, seed) for key, (drift, seed, _) in UNIVERSE.items()}
    columns["SPY"] = _series(spy_drift, 99, noise=0.004)
    columns["^VIX"] = [vix] * len(index)
    columns["CASHX"] = list(np.linspace(100, 103, len(index)))
    return pd.DataFrame(columns, index=index)


def _payload_series(frame: pd.DataFrame, key: str, role: str, column: str | None = None) -> dict:
    values = frame[column or key]
    return {
        "key": key,
        "role": role,
        "dates": [d.date().isoformat() for d in frame.index],
        "adjustedClose": [None if pd.isna(v) else float(v) for v in values],
    }


def _context(frame: pd.DataFrame, *, holdings: list[dict] | None = None, parameters: dict | None = None, as_of: date = AS_OF) -> dict:
    series = [_payload_series(frame, key, "instrument") for key in UNIVERSE]
    series.append(_payload_series(frame, "CASHX", "instrument"))
    series.append(_payload_series(frame, "SPY-US", "benchmark", column="SPY"))
    series.append(_payload_series(frame, "VIX", "volatilityIndex", column="^VIX"))
    return {
        "asOf": as_of.isoformat(),
        "parameters": parameters or {},
        "universe": [{"key": key, "name": f"ETF {key}", "group": group} for key, (_, _, group) in UNIVERSE.items()],
        "cashInstrumentKey": "CASHX",
        "marketSeries": series,
        "subPortfolio": {"currency": "EUR", "cash": 0.0, "contribution": 227.0, "holdings": holdings or []},
    }


def _direct_scan(frame: pd.DataFrame, settings: ScannerSettings, current_positions: list[str] | None = None):
    return scan_etfs(
        frame,
        settings=settings,
        etfs=list(UNIVERSE),
        current_positions=current_positions or [],
        groups={key: group for key, (_, _, group) in UNIVERSE.items()},
        names={key: f"ETF {key}" for key in UNIVERSE},
        cash_etf="CASHX",
        now=pd.Timestamp(AS_OF),
    )


def test_health(client: TestClient) -> None:
    response = client.get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_catalog_lists_etf_momentum_with_parameter_schema(client: TestClient) -> None:
    response = client.get("/v1/strategies")

    assert response.status_code == 200
    [descriptor] = [item for item in response.json() if item["code"] == "etf-momentum"]
    assert descriptor["engine"] == "python"
    assert descriptor["supportsState"] is False
    assert set(descriptor["requiredSeriesRoles"]) == {"instrument", "benchmark", "volatilityIndex"}
    assert "scanner" in descriptor["parametersSchema"]["properties"]
    assert descriptor["defaultParameters"]["scanner"]["topN"] == ScannerSettings.top_n
    assert descriptor["defaultParameters"]["risk"]["maExitBand"] == RiskSettings.ma_exit_band
    assert descriptor["defaultParameters"]["weakMarketInvestPct"] == 0.5


def test_decide_matches_scanner_on_same_data(client: TestClient) -> None:
    frame = _frame()

    response = client.post("/v1/strategies/etf-momentum/decide", json=_context(frame))

    assert response.status_code == 200, response.text
    decision = response.json()
    scan = _direct_scan(frame, ScannerSettings())
    assert [t["key"] for t in decision["targets"]] == list(scan.eligible.index)
    assert all(t["weight"] == pytest.approx(1 / ScannerSettings.top_n) for t in decision["targets"])
    assert decision["market"]["isFavorable"] is scan.market_ok is True
    assert decision["investFraction"] == 1.0
    assert decision["asOf"] == AS_OF.isoformat()
    assert decision["strategyCode"] == "etf-momentum"
    scores = {score["key"]: score for score in decision["rationale"]["scores"]}
    assert scores["FFF"]["status"] == "failed"
    assert scores["AAA"]["momentum"] == pytest.approx(scan.table.loc["AAA", "Momentum"])


def test_weak_market_invests_only_part_of_contribution(client: TestClient) -> None:
    frame = _frame(vix=40.0)

    decision = client.post(
        "/v1/strategies/etf-momentum/decide", json=_context(frame, parameters={"weakMarketInvestPct": 0.3})
    ).json()

    assert decision["market"]["isFavorable"] is False
    assert decision["investFraction"] == pytest.approx(0.3)
    assert any("VIX" in reason for reason in decision["market"]["reasons"])


def test_held_positions_keep_priority_through_hold_buffer(client: TestClient) -> None:
    frame = _frame()
    settings = ScannerSettings(top_n=2)
    holdings = [{"key": "DDD", "lots": [{"quantity": 5, "price": 90.0, "acquiredOn": "2026-03-02"}]}]

    decision = client.post(
        "/v1/strategies/etf-momentum/decide",
        json=_context(frame, holdings=holdings, parameters={"scanner": {"topN": 2}}),
    ).json()

    scan = _direct_scan(frame, settings, current_positions=["DDD"])
    assert [t["key"] for t in decision["targets"]] == list(scan.eligible.index)
    assert "DDD" in [t["key"] for t in decision["targets"]]


def test_series_after_as_of_are_ignored(client: TestClient) -> None:
    frame = _frame()
    later = frame.copy()
    later.index = later.index + pd.offsets.BDay(30)
    combined = pd.concat([frame, later[later.index > frame.index[-1]] * 0.5])

    decision = client.post("/v1/strategies/etf-momentum/decide", json=_context(combined)).json()

    scan = _direct_scan(frame, ScannerSettings())
    assert [t["key"] for t in decision["targets"]] == list(scan.eligible.index)
    assert decision["asOf"] == AS_OF.isoformat()


def test_unknown_strategy_is_not_found(client: TestClient) -> None:
    response = client.post("/v1/strategies/does-not-exist/decide", json=_context(_frame()))

    assert response.status_code == 404


def test_invalid_parameters_are_rejected(client: TestClient) -> None:
    response = client.post(
        "/v1/strategies/etf-momentum/decide", json=_context(_frame(), parameters={"scanner": {"topN": 0}})
    )

    assert response.status_code == 422


def test_unknown_parameter_is_rejected(client: TestClient) -> None:
    response = client.post("/v1/strategies/etf-momentum/decide", json=_context(_frame(), parameters={"topn": 3}))

    assert response.status_code == 422


def test_series_with_mismatched_lengths_are_rejected(client: TestClient) -> None:
    context = _context(_frame())
    context["marketSeries"][0]["adjustedClose"].pop()

    response = client.post("/v1/strategies/etf-momentum/decide", json=context)

    assert response.status_code == 422


def test_missing_benchmark_series_is_rejected(client: TestClient) -> None:
    context = _context(_frame())
    context["marketSeries"] = [s for s in context["marketSeries"] if s["role"] != "benchmark"]

    response = client.post("/v1/strategies/etf-momentum/decide", json=context)

    assert response.status_code == 422


def test_risk_matches_risk_monitor_and_emits_exit_signal(client: TestClient) -> None:
    frame = _frame()
    # FFF fell ~30%: bought near the start of the window, far above today's price.
    entry = float(frame["FFF"].iloc[0])
    holdings = [
        {"key": "FFF", "lots": [{"quantity": 10, "price": entry, "acquiredOn": frame.index[0].date().isoformat()}]},
        {"key": "AAA", "lots": [{"quantity": 2, "price": float(frame["AAA"].iloc[-60]), "acquiredOn": None}]},
    ]

    response = client.post("/v1/strategies/etf-momentum/risk", json=_context(frame, holdings=holdings))

    assert response.status_code == 200, response.text
    assessment = response.json()
    direct = analyze_portfolio_risk(
        frame,
        [
            Position("FFF", entry, 10, frame.index[0].date().isoformat(), "EUR"),
            Position("AAA", float(frame["AAA"].iloc[-60]), 2, None, "EUR"),
        ],
        RiskSettings(),
        cash=0.0,
        cash_etf="CASHX",
    )
    assert assessment["status"] == direct.status == "act"
    positions = {p["key"]: p for p in assessment["positions"]}
    assert positions["FFF"]["status"] == "red"
    assert positions["FFF"]["exitPrice"] == pytest.approx(next(r for r in direct.position_risks if r.ticker == "FFF").exit_price)
    signals = {s["key"]: s for s in assessment["signals"]}
    assert signals["FFF"]["kind"] == "exit"
    # FFF's crash pulls the whole portfolio below its drawdown limit, so every position exits.
    assert assessment["portfolio"]["exitLimitHit"] is direct.portfolio_exit_hit is True
    assert signals["AAA"]["kind"] == "exit"
    assert "нижче свого піку" in signals["AAA"]["reason"]


def test_prices_returns_close_and_adjusted_close(client: TestClient) -> None:
    def fake_downloader(symbols: list[str], start: date, end: date | None) -> dict[str, pd.DataFrame]:
        index = pd.to_datetime(["2026-10-01", "2026-10-02"])
        return {"SXR8.DE": pd.DataFrame({"close": [600.0, 605.5], "adjusted_close": [600.0, 605.5]}, index=index)}

    app = create_app()
    app.dependency_overrides[get_quote_downloader] = lambda: fake_downloader
    response = TestClient(app).post("/v1/prices", json={"symbols": ["SXR8.DE", "NOPE.DE"], "start": "2026-10-01"})

    assert response.status_code == 200, response.text
    body = response.json()
    [series] = body["series"]
    assert series == {
        "symbol": "SXR8.DE",
        "dates": ["2026-10-01", "2026-10-02"],
        "close": [600.0, 605.5],
        "adjustedClose": [600.0, 605.5],
    }
    assert body["missing"] == ["NOPE.DE"]


def test_published_contract_matches_snapshot(client: TestClient) -> None:
    """The OpenAPI document is the contract with .NET; regenerate with tools/export_contract.py."""
    current = client.get("/openapi.json").json()

    assert CONTRACT_PATH.exists(), "Run: uv run python tools/export_contract.py"
    assert json.loads(CONTRACT_PATH.read_text(encoding="utf-8")) == current


def test_parameter_models_cover_every_setting() -> None:
    from dataclasses import fields

    from finance_decisions.strategies.etf_momentum.parameters import RiskParameters, ScannerParameters

    assert set(ScannerParameters.model_fields) == {f.name for f in fields(ScannerSettings)}
    assert set(RiskParameters.model_fields) == {f.name for f in fields(RiskSettings)}
    assert ScannerParameters().to_settings() == ScannerSettings()
    assert RiskParameters().to_settings() == RiskSettings()


def test_risk_warns_without_selling_when_limits_are_not_hit(client: TestClient) -> None:
    frame = _frame()
    # Bought above today's price: a small loss triggers a warning, not an exit.
    entry = float(frame["AAA"].iloc[-1]) * 1.05
    holdings = [{"key": "AAA", "lots": [{"quantity": 2, "price": entry, "acquiredOn": None}]}]

    assessment = client.post("/v1/strategies/etf-momentum/risk", json=_context(frame, holdings=holdings)).json()

    assert assessment["status"] == "watch"
    assert assessment["portfolio"]["exitLimitHit"] is False
    [signal] = assessment["signals"]
    assert signal["key"] == "AAA"
    assert signal["kind"] == "warn"
