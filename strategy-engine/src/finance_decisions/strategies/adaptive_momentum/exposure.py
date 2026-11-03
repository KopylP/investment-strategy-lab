from __future__ import annotations

from dataclasses import dataclass

import pandas as pd

from finance_decisions.portfolio_math import portfolio_volatility


@dataclass(frozen=True)
class Exposure:
    share: float
    volatility: float | None
    target: float

    @property
    def reduced(self) -> bool:
        return self.share < 1.0


def exposure_for(prices: pd.DataFrame, weights: dict[str, float], target: float, window_days: int) -> Exposure:
    volatility = portfolio_volatility(prices, weights, window_days)
    if volatility is None or volatility <= target:
        return Exposure(1.0, volatility, target)

    return Exposure(target / volatility, volatility, target)
