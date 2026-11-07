from __future__ import annotations

import math

import numpy as np
import pandas as pd
import pytest

from finance_decisions.portfolio_math import pairwise_correlation


def _pandas_correlation(prices: pd.DataFrame, first: str, second: str, window_days: int, min_days: int = 60) -> float:
    recent = prices.tail(window_days + 1)
    returns = recent[[first, second]].dropna().pct_change().dropna()
    if len(returns) < min(min_days, window_days):
        return float("nan")

    return float(returns[first].corr(returns[second]))


def _prices(seed: int, days: int = 300) -> pd.DataFrame:
    generator = np.random.default_rng(seed)
    walks = 100 * np.exp(np.cumsum(generator.normal(0, 0.01, size=(days, 3)), axis=0))
    frame = pd.DataFrame(walks, columns=["a", "b", "c"], index=pd.bdate_range("2023-01-02", periods=days))
    frame.iloc[generator.choice(days, 25, replace=False), 0] = np.nan
    frame.iloc[generator.choice(days, 25, replace=False), 1] = np.nan
    frame.iloc[:40, 2] = np.nan
    return frame


@pytest.mark.parametrize("seed", range(5))
@pytest.mark.parametrize(("first", "second", "window"), [("a", "b", 126), ("a", "c", 252), ("b", "c", 63), ("a", "b", 400)])
def test_the_correlation_equals_the_pandas_definition_with_gaps(seed: int, first: str, second: str, window: int) -> None:
    prices = _prices(seed)

    expected = _pandas_correlation(prices, first, second, window)
    actual = pairwise_correlation(prices, first, second, window)

    assert actual == pytest.approx(expected, abs=1e-12, nan_ok=True)


def test_too_few_common_days_give_no_correlation() -> None:
    prices = _prices(1)
    prices.iloc[:-30, 2] = np.nan

    assert math.isnan(pairwise_correlation(prices, "a", "c", 126))


def test_a_flat_series_has_no_correlation() -> None:
    prices = _prices(2)
    prices["c"] = 100.0

    assert math.isnan(pairwise_correlation(prices, "a", "c", 126))
    assert math.isnan(_pandas_correlation(prices, "a", "c", 126))
