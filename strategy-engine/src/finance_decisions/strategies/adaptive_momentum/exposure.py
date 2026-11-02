from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd

from .fund_reading import TRADING_DAYS_PER_YEAR

MIN_VOLATILITY_DAYS = 5


@dataclass(frozen=True)
class Exposure:
    share: float
    volatility: float | None
    target: float

    @property
    def reduced(self) -> bool:
        return self.share < 1.0


def exposure_for(prices: pd.DataFrame, weights: dict[str, float], target: float, window_days: int) -> Exposure:
    volatility = _portfolio_volatility(prices, weights, window_days)
    if volatility is None or volatility <= target:
        return Exposure(1.0, volatility, target)

    return Exposure(target / volatility, volatility, target)


def _portfolio_volatility(prices: pd.DataFrame, weights: dict[str, float], window_days: int) -> float | None:
    if not weights:
        return None

    keys = list(weights)
    recent = prices[keys].tail(window_days + 1).ffill()
    daily = recent.pct_change().dropna(how="all").fillna(0.0)
    if len(daily) < MIN_VOLATILITY_DAYS:
        return None

    portfolio = daily @ np.array([weights[key] for key in keys])

    return float(portfolio.std() * np.sqrt(TRADING_DAYS_PER_YEAR))
