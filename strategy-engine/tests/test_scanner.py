from __future__ import annotations

import numpy as np
import pandas as pd
import pytest

from finance_decisions.helpers import completed_month_closes
from finance_decisions.strategies.etf_momentum.config import ScannerSettings
from finance_decisions.strategies.etf_momentum.scanner import scan_etfs
from finance_decisions.messages import render


def _prices(columns: dict, periods: int = 320, start: str = "2024-01-01") -> pd.DataFrame:
    dates = pd.date_range(start, periods=periods, freq="B")
    base = {
        "SPY": [100 + index * 0.1 for index in range(periods)],
        "^VIX": [18.0] * periods,
    }
    base.update({name: values(periods) if callable(values) else values for name, values in columns.items()})
    return pd.DataFrame(base, index=dates)


def _trend(start: float, step: float):
    return lambda n: [start + index * step for index in range(n)]


def test_scan_etfs_returns_top_candidate() -> None:
    prices = _prices({"AAA": _trend(50, 0.1), "BBB": _trend(60, 0.03)})

    result = scan_etfs(prices, ScannerSettings(top_n=1, lookback_months=3, ma_period=20))

    assert result.market_ok is True
    assert list(result.eligible.index) == ["AAA"]
    assert result.eligible.loc["AAA", "Weight"] == 1.0


def test_scan_etfs_ignores_sparse_missing_values_for_ma200() -> None:
    prices = _prices({"AAA": _trend(50, 0.1)})
    prices.loc[prices.index[150], "SPY"] = None
    prices.loc[prices.index[175], "AAA"] = None

    result = scan_etfs(prices, ScannerSettings(top_n=1, lookback_months=3, ma_period=200))

    assert result.spy_above_ma is True
    assert bool(result.table.loc["AAA", "Above MA200"]) is True
    assert list(result.eligible.index) == ["AAA"]


def test_scan_etfs_sharpe_filter_and_hold_buffer() -> None:
    prices = _prices(
        {
            "AAA": _trend(100, 0.5),
            "BBB": _trend(100, 0.4),
            "CCC": _trend(100, 0.3),
            "DDD": _trend(100, 0.25),
            "EEE": _trend(100, 0.2),
            # Higher return than DDD/EEE but wild swings -> fails volatility and Sharpe.
            "FFF": lambda n: [100 + index * 0.28 + (100 if index % 2 == 0 else -100) for index in range(n)],
        }
    )
    settings = ScannerSettings(top_n=3, lookback_months=3, ma_period=20, min_sharpe=2.0, hold_buffer_k=4)

    no_positions = scan_etfs(prices, settings)
    assert list(no_positions.eligible.index) == ["AAA", "BBB", "CCC"]
    assert no_positions.table.loc["FFF", "Status"] == "failed"
    assert "волатильність" in render(no_positions.table.loc["FFF", "Reason"])

    # DDD is held and ranks 4 <= buffer 4, so it keeps its slot and pushes CCC out.
    with_ddd = scan_etfs(prices, settings, current_positions=["DDD"])
    assert list(with_ddd.eligible.index) == ["AAA", "BBB", "DDD"]
    assert with_ddd.table.loc["DDD", "Status"] == "buffer"
    assert with_ddd.table.loc["CCC", "Status"] == "reserve"

    # EEE ranks 5 > buffer 4, so holding it does not protect it.
    with_eee = scan_etfs(prices, settings, current_positions=["EEE"])
    assert list(with_eee.eligible.index) == ["AAA", "BBB", "CCC"]


def test_missing_vix_on_last_row_still_uses_last_valid_close() -> None:
    prices = _prices({"AAA.DE": _trend(10, 0.05)})
    prices["^VIX"] = 45.0
    prices.loc[prices.index[-1], ["SPY", "^VIX"]] = np.nan  # US closed, Xetra traded

    result = scan_etfs(prices, ScannerSettings(ma_period=20), etfs=["AAA.DE"])

    assert result.vix == 45.0
    assert result.market_ok is False


def test_unavailable_vix_fails_market_check() -> None:
    prices = _prices({"AAA": _trend(10, 0.05)}).drop(columns=["^VIX"])

    result = scan_etfs(prices, ScannerSettings(ma_period=20), etfs=["AAA"])

    assert result.vix is None
    assert result.market_ok is False
    assert any("VIX" in render(reason) for reason in result.market_reasons)


def test_negative_momentum_is_rejected_even_above_ma() -> None:
    # Rises for a year, then drifts down for four months while staying above the MA.
    def rise_then_dip(n: int) -> list[float]:
        return [100 + index * 0.5 if index < n - 90 else 100 + (n - 90) * 0.5 - (index - (n - 90)) * 0.05 for index in range(n)]

    prices = _prices({"AAA": rise_then_dip, "BBB": _trend(100, 0.2)})

    result = scan_etfs(prices, ScannerSettings(lookback_months=3, ma_period=200, min_sharpe=-10))

    assert result.table.loc["AAA", "Above MA200"]
    assert result.table.loc["AAA", "Momentum"] < 0
    assert result.table.loc["AAA", "Status"] == "failed"
    assert list(result.eligible.index) == ["BBB"]


def test_group_cap_keeps_one_etf_per_group() -> None:
    prices = _prices({"NDX": _trend(100, 0.5), "TECH": _trend(100, 0.45), "GOLD": _trend(100, 0.2)})
    groups = {"NDX": "US tech", "TECH": "US tech", "GOLD": "Gold"}

    result = scan_etfs(prices, ScannerSettings(top_n=3, lookback_months=3, ma_period=20), groups=groups)

    assert list(result.eligible.index) == ["NDX", "GOLD"]
    assert result.table.loc["TECH", "Status"] == "group_blocked"
    assert result.cash_weight == pytest.approx(1 / 3)


def test_stale_ticker_is_excluded() -> None:
    prices = _prices({"AAA": _trend(100, 0.5), "OLD": _trend(100, 0.6)})
    prices.loc[prices.index[-15]:, "OLD"] = np.nan

    result = scan_etfs(prices, ScannerSettings(top_n=2, lookback_months=3, ma_period=20))

    assert result.table.loc["OLD", "Status"] == "failed"
    assert "застарілі" in render(result.table.loc["OLD", "Reason"])
    assert list(result.eligible.index) == ["AAA"]


def test_completed_month_closes_drops_month_in_progress() -> None:
    series = pd.Series(1.0, index=pd.date_range("2026-01-01", "2026-03-11", freq="B"))

    assert completed_month_closes(series, now=pd.Timestamp("2026-03-12")).index[-1].month == 2
    # Once the calendar moves on, the last month counts even without a final business-day close.
    assert completed_month_closes(series, now=pd.Timestamp("2026-04-02")).index[-1].month == 3
    full = pd.Series(1.0, index=pd.date_range("2026-01-01", "2026-03-31", freq="B"))
    assert completed_month_closes(full, now=pd.Timestamp("2026-03-31")).index[-1].month == 3


def test_larger_holding_wins_within_a_group() -> None:
    prices = _prices({"NDX": _trend(100, 0.4), "TECH": _trend(100, 0.5), "GOLD": _trend(100, 0.2)})
    groups = {"NDX": "US tech", "TECH": "US tech", "GOLD": "Gold"}
    settings = ScannerSettings(top_n=3, lookback_months=3, ma_period=20)

    # NDX is the bigger holding, so it keeps the tech slot although TECH ranks higher.
    result = scan_etfs(prices, settings, current_positions=["NDX", "TECH"], groups=groups)

    assert "NDX" in result.eligible.index
    assert result.table.loc["TECH", "Status"] == "group_blocked"


def test_correlation_limit_skips_a_twin_of_a_chosen_etf() -> None:
    days = 320
    noise = np.random.default_rng(1).normal(0, 0.01, (3, days))
    drift = 1.002
    lead = 100 * np.cumprod(drift + noise[0])
    twin = 100 * np.cumprod(drift - 0.0002 + noise[0] + noise[1] * 0.05)
    other = 100 * np.cumprod(drift - 0.0004 + noise[2])
    prices = _prices({"LEAD": list(lead), "TWIN": list(twin), "OTHER": list(other)})
    settings = ScannerSettings(top_n=2, lookback_months=3, ma_period=20, max_volatility=2.0, max_drawdown=-1.0, min_sharpe=-99.0, max_correlation=0.7)

    result = scan_etfs(prices, settings, current_positions=["LEAD"])

    assert set(result.eligible.index) == {"LEAD", "OTHER"}
    assert result.table.loc["TWIN", "Status"] == "correlated"
