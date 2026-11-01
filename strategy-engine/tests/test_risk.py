from __future__ import annotations

import numpy as np
import pandas as pd
import pytest

from finance_decisions.models import Position
from finance_decisions.strategies.etf_momentum.config import RiskSettings
from finance_decisions.strategies.etf_momentum.risk import analyze_portfolio_risk, portfolio_drawdown
from finance_decisions.messages import render


def _frame(qqq: list[float], periods: int = 260, vix: float = 18.0) -> pd.DataFrame:
    dates = pd.date_range("2025-01-01", periods=periods, freq="B")
    return pd.DataFrame(
        {"SPY": [100 + index * 0.1 for index in range(periods)], "^VIX": [vix] * periods, "QQQ": qqq},
        index=dates,
    )


def test_analyze_portfolio_risk_flags_stop_loss() -> None:
    prices = _frame([100.0] * 259 + [90.0])

    report = analyze_portfolio_risk(prices, [Position(ticker="QQQ", entry_price=100.0, shares=2)])

    assert report.portfolio_pnl == pytest.approx(-0.10)
    assert report.position_risks[0].status == "red"
    assert report.position_risks[0].sell is True
    assert render(report.actions[0]).startswith("Продати QQQ повністю")
    assert report.status == "act"


def test_analyze_portfolio_risk_ignores_sparse_missing_values_for_ma200() -> None:
    prices = _frame([100 + index * 0.1 for index in range(260)])
    prices.loc[prices.index[120], "SPY"] = None
    prices.loc[prices.index[140], "QQQ"] = None

    report = analyze_portfolio_risk(prices, [Position(ticker="QQQ", entry_price=100.0, shares=2)])

    assert report.spy_above_ma is True
    assert report.position_risks[0].above_ma is True
    assert report.position_risks[0].status == "green"
    assert report.status == "ok"


def test_price_just_below_ma_is_a_warning_not_a_sale() -> None:
    qqq = [120.0] * 250 + [119.0] * 10
    prices = _frame(qqq)
    risk = analyze_portfolio_risk(prices, [Position("QQQ", 117.0, 1)], RiskSettings(ma_period=20, small_loss_warn=-0.5)).position_risks[0]

    assert risk.ma_value > risk.current_price > risk.ma_exit_price
    assert risk.status == "yellow"
    assert risk.sell is False
    assert risk.exit_price is not None and risk.exit_price < risk.current_price


def test_exit_price_is_the_highest_trigger_level() -> None:
    prices = _frame([100 + index * 0.1 for index in range(260)])
    risk = analyze_portfolio_risk(prices, [Position("QQQ", 120.0, 1)]).position_risks[0]

    levels = [level for level in (risk.stop_price, risk.ma_exit_price, risk.trail_exit_price) if level is not None]
    assert risk.exit_price == max(levels)
    assert risk.exit_distance == pytest.approx(risk.exit_price / risk.current_price - 1)


def test_vix_read_from_last_valid_close() -> None:
    prices = _frame([100 + index * 0.1 for index in range(260)], vix=40.0)
    prices.loc[prices.index[-1], ["SPY", "^VIX"]] = np.nan

    report = analyze_portfolio_risk(prices, [Position("QQQ", 100.0, 1)])

    assert report.vix == 40.0
    assert any("VIX" in render(alert) for alert in report.market_alerts)


def test_lots_are_combined_per_ticker() -> None:
    prices = _frame([100.0] * 260)
    lots = [Position("QQQ", 90.0, 1, "2025-03-03"), Position("QQQ", 110.0, 1, "2025-06-02")]

    report = analyze_portfolio_risk(prices, lots)

    assert len(report.position_risks) == 1
    assert report.position_risks[0].shares == 2
    assert report.position_risks[0].entry_price == pytest.approx(100.0)


def test_portfolio_drawdown_measured_from_peak_not_cost() -> None:
    # Doubles, then falls 30%: still +40% vs cost, but -30% from the peak.
    qqq = [100.0] * 50 + list(np.linspace(100, 200, 100)) + list(np.linspace(200, 140, 110))
    prices = _frame(qqq)
    lots = [Position("QQQ", 100.0, 1, "2025-01-01")]

    drawdown, _ = portfolio_drawdown(prices, lots)
    report = analyze_portfolio_risk(prices, lots)

    assert drawdown == pytest.approx(-0.30, abs=1e-6)
    assert report.portfolio_pnl == pytest.approx(0.40)
    assert report.portfolio_exit_hit is True
    assert any("Продати всі позиції" in render(action) for action in report.actions)


def test_new_contribution_does_not_hide_drawdown() -> None:
    qqq = [100.0] * 200 + [80.0] * 60
    prices = _frame(qqq)
    lots = [Position("QQQ", 100.0, 1, "2025-01-01"), Position("QQQ", 80.0, 10, prices.index[-30].date().isoformat())]

    drawdown, _ = portfolio_drawdown(prices, lots)

    assert drawdown == pytest.approx(-0.20)


def test_cash_etf_position_has_no_exit_rules() -> None:
    prices = _frame([100.0] * 260)
    prices["XEON.DE"] = np.linspace(140, 143, 260)

    report = analyze_portfolio_risk(prices, [Position("XEON.DE", 140.0, 2)], cash_etf="XEON.DE")

    assert report.position_risks[0].status == "cash"
    assert report.actions == []
    assert report.portfolio_value == pytest.approx(286.0)


def test_broken_position_only_warns_when_position_exits_are_off() -> None:
    prices = _frame([100.0] * 259 + [90.0])
    settings = RiskSettings(sell_on_position_exit=False)

    report = analyze_portfolio_risk(prices, [Position(ticker="QQQ", entry_price=100.0, shares=2)], settings)

    assert report.position_risks[0].status == "red"
    assert report.position_risks[0].sell is False
    assert report.status == "watch"
