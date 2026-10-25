from __future__ import annotations

import json

import numpy as np
import pandas as pd
import pytest

from finance_decisions.strategies.etf_momentum import golden
from finance_decisions.strategies.etf_momentum.backtest import run_backtest
from finance_decisions.strategies.etf_momentum.config import (
    BacktestSettings,
    PlanSettings,
    RiskSettings,
    ScannerSettings,
    StrategyConfig,
)


def _orders(plan) -> list[dict]:
    return [
        {"action": o.action, "kind": o.kind, "ticker": o.ticker, "amount": o.amount, "shares": o.shares, "price": o.price}
        for o in plan.orders
    ]


@pytest.mark.parametrize("fixture", golden.synthetic_cases(), ids=lambda fixture: fixture["name"])
def test_synthetic_fixture_replays_to_its_expected_orders(fixture: dict) -> None:
    replayed = golden.replay(json.loads(json.dumps(fixture)))

    expected = [{k: order[k] for k in ("action", "kind", "ticker", "amount", "shares", "price")} for order in fixture["expected"]["orders"]]
    assert _orders(replayed) == expected
    assert replayed.parked_after == pytest.approx(fixture["expected"]["parkedAfter"])


def test_synthetic_cases_cover_every_order_kind() -> None:
    kinds = {order["kind"] for fixture in golden.synthetic_cases() for order in fixture["expected"]["orders"]}

    assert kinds == {"exit", "trim", "unpark", "buy", "park"}


def test_synthetic_case_names_are_unique() -> None:
    names = [fixture["name"] for fixture in golden.synthetic_cases()]

    assert len(names) == len(set(names))


def test_fixture_states_market_regime_and_invest_fraction() -> None:
    weak = next(f for f in golden.synthetic_cases() if f["name"] == "weak-market-invests-part-and-parks-rest")

    assert weak["decision"]["marketOk"] is False
    assert weak["decision"]["investFraction"] == pytest.approx(weak["settings"]["weakMarketInvestPct"])


def _backtest_prices(periods: int = 700) -> pd.DataFrame:
    index = pd.bdate_range(end="2026-09-30", periods=periods)
    rng = np.random.default_rng(7)

    def path(drift: float) -> np.ndarray:
        return 50 * np.exp(np.cumsum(drift / 252 + rng.normal(0, 0.008, periods)))

    return pd.DataFrame(
        {
            "SPY": path(0.12),
            "^VIX": np.full(periods, 17.0),
            "AAA": path(0.30),
            "BBB": path(0.20),
            "CCC": path(0.10),
            "CASHX": np.linspace(100, 106, periods),
        },
        index=index,
    )


def test_backtest_cases_capture_one_fixture_per_month_that_replays() -> None:
    config = StrategyConfig(
        etfs=["AAA", "BBB", "CCC"],
        risk=RiskSettings(),
        scanner=ScannerSettings(top_n=2, hold_buffer_k=3, ma_period=50, lookback_months=3, min_sharpe=0.0),
        plan=PlanSettings(monthly_contribution=227.0),
        backtest=BacktestSettings(years=1, benchmark="AAA"),
        cash_etf="CASHX",
    )
    prices = _backtest_prices()

    fixtures = golden.backtest_cases(prices, config, months=6, label="test")

    assert len(fixtures) == 6
    assert [f["asOf"] for f in fixtures] == sorted(f["asOf"] for f in fixtures)
    for fixture in fixtures:
        assert _orders(golden.replay(fixture)) == [
            {k: order[k] for k in ("action", "kind", "ticker", "amount", "shares", "price")} for order in fixture["expected"]["orders"]
        ]


def test_backtest_observer_sees_every_monthly_plan() -> None:
    config = StrategyConfig(
        etfs=["AAA", "BBB", "CCC"],
        risk=RiskSettings(),
        scanner=ScannerSettings(top_n=2, hold_buffer_k=3, ma_period=50, lookback_months=3, min_sharpe=0.0),
        plan=PlanSettings(monthly_contribution=100.0),
        backtest=BacktestSettings(years=1, benchmark="AAA"),
        cash_etf="CASHX",
    )
    captured = []

    result = run_backtest(_backtest_prices(), config, plan_observer=captured.append)

    assert len(captured) == result.months
    assert all(capture.contribution == 100.0 for capture in captured)
