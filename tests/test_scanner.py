from __future__ import annotations

import pandas as pd

from finance_decisions.strategies.etf_momentum.config import ScannerSettings
from finance_decisions.strategies.etf_momentum.scanner import scan_etfs


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


def test_scan_etfs_sharpe_filter_and_hold_buffer() -> None:
    dates = pd.date_range("2024-01-01", periods=320, freq="B")
    prices = pd.DataFrame(
        {
            "SPY": [100 + index * 0.1 for index in range(320)],
            "^VIX": [18.0] * 320,
            "AAA": [100 + index * 0.5 for index in range(320)],  # Rank 1 momentum
            "BBB": [100 + index * 0.4 for index in range(320)],  # Rank 2 momentum
            "CCC": [100 + index * 0.3 for index in range(320)],  # Rank 3 momentum
            "DDD": [100 + index * 0.25 for index in range(320)], # Rank 4 momentum (under buffer)
            "EEE": [100 + index * 0.2 for index in range(320)],  # Rank 5 momentum (not under buffer)
            # FFF has higher return than DDD/EEE but wild volatility -> low Sharpe
            "FFF": [100 + index * 0.28 + (100 if index % 2 == 0 else -100) for index in range(320)],
        },
        index=dates,
    )

    settings = ScannerSettings(
        top_n=3,
        lookback_months=3,
        ma_period=20,
        min_sharpe=2.0,
        hold_buffer_k=4,
    )

    # 1. No current positions: should select AAA, BBB, CCC (Top 3)
    result_no_positions = scan_etfs(prices, settings)
    assert list(result_no_positions.eligible.index) == ["AAA", "BBB", "CCC"]
    # FFF must be filtered out because of low Sharpe ratio
    assert "FFF" not in result_no_positions.eligible.index
    assert result_no_positions.table.loc["FFF", "Sharpe"] < 2.0

    # 2. Current position includes DDD (Rank 4):
    # Since DDD is in current positions and its rank is 4 <= hold_buffer_k (4),
    # it should be kept, displacing CCC (rank 3) out of the selected top_n list.
    result_with_positions = scan_etfs(prices, settings, current_positions=["DDD"])
    assert "DDD" in result_with_positions.eligible.index
    # The selected should be AAA, BBB, and DDD
    assert list(result_with_positions.eligible.index) == ["DDD", "AAA", "BBB"]

    # 3. Current position includes EEE (Rank 5):
    # EEE rank is 5 > hold_buffer_k (4), so it should NOT be kept.
    result_with_eee = scan_etfs(prices, settings, current_positions=["EEE"])
    assert "EEE" not in result_with_eee.eligible.index
    assert list(result_with_eee.eligible.index) == ["AAA", "BBB", "CCC"]

