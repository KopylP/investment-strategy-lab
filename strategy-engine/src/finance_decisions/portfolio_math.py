from __future__ import annotations

import numpy as np
import pandas as pd

TRADING_DAYS_PER_MONTH = 21
TRADING_DAYS_PER_YEAR = 252


def pairwise_correlation(prices: pd.DataFrame, first: str, second: str, window_days: int, min_days: int = 60) -> float:
    recent = prices.tail(window_days + 1)
    returns = recent[[first, second]].dropna().pct_change().dropna()
    if len(returns) < min_days:
        return float("-inf")

    return float(returns[first].corr(returns[second]))


def portfolio_volatility(prices: pd.DataFrame, weights: dict[str, float], window_days: int, min_days: int = 5) -> float | None:
    if not weights:
        return None

    keys = list(weights)
    recent = prices[keys].tail(window_days + 1).ffill()
    daily = recent.pct_change().dropna(how="all").fillna(0.0)
    if len(daily) < min_days:
        return None

    portfolio = daily @ np.array([weights[key] for key in keys])

    return float(portfolio.std() * np.sqrt(TRADING_DAYS_PER_YEAR))
