from __future__ import annotations

import pandas as pd
import pytest

from finance_decisions.models import Position
from finance_decisions.risk import analyze_portfolio_risk


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
    assert "Sell QQQ" in report.actions[0]
