from __future__ import annotations

import pandas as pd

from finance_decisions.config import ScannerSettings
from finance_decisions.scanner import scan_etfs


def test_scan_etfs_returns_top_candidate() -> None:
    dates = pd.date_range("2024-01-01", periods=320, freq="B")
    prices = pd.DataFrame(
        {
            "SPY": [100 + index * 0.1 for index in range(320)],
            "^VIX": [18.0] * 320,
            "AAA": [50 + index * 0.1 for index in range(320)],
            "BBB": [60 + index * 0.03 for index in range(320)],
        },
        index=dates,
    )

    result = scan_etfs(
        prices,
        ScannerSettings(top_n=1, lookback_months=3, ma_period=20),
    )

    assert result.market_ok is True
    assert list(result.eligible.index) == ["AAA"]


def test_scan_etfs_ignores_sparse_missing_values_for_ma200() -> None:
    dates = pd.date_range("2024-01-01", periods=320, freq="B")
    prices = pd.DataFrame(
        {
            "SPY": [100 + index * 0.1 for index in range(320)],
            "^VIX": [18.0] * 320,
            "AAA": [50 + index * 0.1 for index in range(320)],
        },
        index=dates,
    )
    prices.loc[dates[150], "SPY"] = None
    prices.loc[dates[175], "AAA"] = None

    result = scan_etfs(
        prices,
        ScannerSettings(top_n=1, lookback_months=3, ma_period=200),
    )

    assert result.spy_above_ma is True
    assert bool(result.table.loc["AAA", "Above MA200"]) is True
    assert list(result.eligible.index) == ["AAA"]
