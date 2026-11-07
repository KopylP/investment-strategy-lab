from __future__ import annotations

import numpy as np
import pandas as pd

TRADING_DAYS_PER_MONTH = 21
TRADING_DAYS_PER_YEAR = 252


def pairwise_correlation(prices: pd.DataFrame, first: str, second: str, window_days: int, min_days: int = 60) -> float:
    recent = prices[[first, second]].to_numpy(dtype=float)[-(window_days + 1):]
    common = recent[~np.isnan(recent).any(axis=1)]
    returns = _daily_returns(common)
    if len(returns) < min(min_days, window_days):
        return float("nan")

    return _correlation(returns[:, 0], returns[:, 1])


def _daily_returns(closes: np.ndarray) -> np.ndarray:
    with np.errstate(divide="ignore", invalid="ignore"):
        returns = closes[1:] / closes[:-1] - 1

    return returns[~np.isnan(returns).any(axis=1)]


def _correlation(first: np.ndarray, second: np.ndarray) -> float:
    first_deviation = first - first.mean()
    second_deviation = second - second.mean()
    spread = np.sqrt((first_deviation ** 2).sum() * (second_deviation ** 2).sum())
    if spread == 0:
        return float("nan")

    return float((first_deviation * second_deviation).sum() / spread)


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
