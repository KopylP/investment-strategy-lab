from __future__ import annotations

import pandas as pd
import pytest

from finance_decisions.models import Position
from finance_decisions.strategies.etf_momentum.risk import analyze_portfolio_risk


def test_analyze_portfolio_risk_flags_stop_loss() -> None:
    dates = pd.date_range("2025-01-01", periods=260, freq="B")
    prices = pd.DataFrame(
        {
            "SPY": [100 + index * 0.1 for index in range(260)],
            "^VIX": [18.0] * 260,
            "QQQ": [100.0] * 259 + [90.0],
        },
        index=dates,
    )

    report = analyze_portfolio_risk(
        prices,
        [Position(ticker="QQQ", entry_price=100.0, shares=2)],
    )

    assert report.portfolio_pnl == pytest.approx(-0.10)
    assert report.position_risks[0].status == "red"
    assert "Продати QQQ" in report.actions[0]


def test_analyze_portfolio_risk_ignores_sparse_missing_values_for_ma200() -> None:
    dates = pd.date_range("2025-01-01", periods=260, freq="B")
    prices = pd.DataFrame(
        {
            "SPY": [100 + index * 0.1 for index in range(260)],
            "^VIX": [18.0] * 260,
            "QQQ": [100 + index * 0.1 for index in range(260)],
        },
        index=dates,
    )
    prices.loc[dates[120], "SPY"] = None
    prices.loc[dates[140], "QQQ"] = None

    report = analyze_portfolio_risk(
        prices,
        [Position(ticker="QQQ", entry_price=100.0, shares=2)],
    )

    assert report.spy_above_ma is True
    assert report.position_risks[0].above_ma is True
    assert report.position_risks[0].status == "green"
