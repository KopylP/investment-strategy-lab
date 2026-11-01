from __future__ import annotations

import numpy as np
import pandas as pd
import pytest

from finance_decisions.strategies.etf_momentum.config import PlanSettings
from finance_decisions.strategies.etf_momentum.plan import build_plan
from finance_decisions.strategies.etf_momentum.scanner import ScanResult
from finance_decisions.messages import render


def _scan(picks: list[str], market_ok: bool = True, others: dict[str, str] | None = None) -> ScanResult:
    rows = []
    for rank, ticker in enumerate(picks, 1):
        rows.append({"ETF": ticker, "Momentum": 0.3 - rank / 100, "Rank": rank, "Status": "selected", "Reason": f"ранг {rank}"})
    for ticker, reason in (others or {}).items():
        rows.append({"ETF": ticker, "Momentum": -0.1, "Rank": np.nan, "Status": "failed", "Reason": reason})
    table = pd.DataFrame(rows).set_index("ETF")
    return ScanResult(market_ok=market_ok, spy_above_ma=market_ok, vix=18.0, table=table, eligible=table.loc[picks])


PRICES = {"AAA": 10.0, "BBB": 20.0, "CCC": 50.0, "DDD": 25.0, "OLD": 40.0, "CASHX": 100.0}
SETTINGS = PlanSettings(monthly_contribution=400.0, min_trade=25.0)


def _plan(scan, holdings=None, cash=0.0, settings=SETTINGS, top_n=4, cost_basis=None, contribution=None):
    return build_plan(
        scan,
        holdings=holdings or {},
        prices=PRICES,
        cash=cash,
        settings=settings,
        top_n=top_n,
        cash_etf="CASHX",
        contribution=contribution,
        cost_basis=cost_basis,
    )


def _amounts(plan, kind):
    return {order.ticker: round(order.amount, 2) for order in plan.orders if order.kind == kind}


def test_new_money_is_split_equally_across_slots() -> None:
    plan = _plan(_scan(["AAA", "BBB", "CCC", "DDD"]))

    assert _amounts(plan, "buy") == {"AAA": 100.0, "BBB": 100.0, "CCC": 100.0, "DDD": 100.0}
    assert plan.orders[0].shares == pytest.approx(10.0)
    assert not plan.sells


def test_unfilled_slots_are_parked_not_concentrated() -> None:
    plan = _plan(_scan(["AAA", "BBB"]))

    assert _amounts(plan, "buy") == {"AAA": 100.0, "BBB": 100.0}
    assert _amounts(plan, "park") == {"CASHX": 200.0}
    assert any("2 з 4" in render(note) for note in plan.notes)


def test_weak_market_invests_half_and_parks_the_rest() -> None:
    plan = _plan(_scan(["AAA", "BBB", "CCC", "DDD"], market_ok=False))

    assert sum(_amounts(plan, "buy").values()) == pytest.approx(200.0)
    assert _amounts(plan, "park") == {"CASHX": 200.0}
    assert plan.invest_pct == 0.5


def test_dropped_position_is_sold_and_proceeds_reinvested() -> None:
    scan = _scan(["AAA", "BBB", "CCC", "DDD"], others={"OLD": "ціна нижче MA200"})
    plan = _plan(scan, holdings={"OLD": 10.0}, contribution=0.0)

    assert plan.orders[0].kind == "exit"
    assert plan.orders[0].ticker == "OLD"
    assert "ціна нижче MA200" in plan.orders[0].reason
    assert sum(_amounts(plan, "buy").values()) == pytest.approx(400.0)


def test_parked_cash_returns_when_market_is_ok() -> None:
    plan = _plan(_scan(["AAA", "BBB", "CCC", "DDD"]), holdings={"CASHX": 4.0}, contribution=0.0)

    assert _amounts(plan, "unpark") == {"CASHX": 400.0}
    assert sum(_amounts(plan, "buy").values()) == pytest.approx(400.0)
    assert [order.kind for order in plan.orders][0] == "unpark"


def test_losing_overweight_position_is_not_trimmed() -> None:
    holdings = {"AAA": 40.0}  # €400 position
    plan = _plan(_scan(["AAA", "BBB", "CCC", "DDD"]), holdings=holdings, cost_basis={"AAA": 12.0})

    assert "AAA" not in _amounts(plan, "trim")
    assert sum(_amounts(plan, "buy").values()) == pytest.approx(400.0, abs=0.05)


def test_winning_overweight_position_is_trimmed() -> None:
    holdings = {"AAA": 40.0}
    plan = _plan(_scan(["AAA", "BBB", "CCC", "DDD"]), holdings=holdings, cost_basis={"AAA": 5.0})

    assert _amounts(plan, "trim") == {"AAA": 200.0}
    assert _amounts(plan, "buy") == {"BBB": 200.0, "CCC": 200.0, "DDD": 200.0}


def test_orders_below_minimum_are_skipped() -> None:
    plan = _plan(_scan(["AAA", "BBB", "CCC", "DDD"]), contribution=80.0)

    assert not _amounts(plan, "buy")
    assert any("дрібні" in render(note) for note in plan.notes)


def test_whole_shares_mode_rounds_down() -> None:
    settings = PlanSettings(monthly_contribution=400.0, fractional_shares=False)
    plan = _plan(_scan(["CCC"]), settings=settings, top_n=1, contribution=120.0)

    buy = plan.buys[0]
    assert buy.shares == 2
    assert buy.amount == pytest.approx(100.0)
