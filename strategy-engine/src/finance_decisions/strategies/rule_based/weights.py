from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd

from finance_decisions.portfolio_math import TRADING_DAYS_PER_YEAR, portfolio_volatility
from .parameters import Weights

MIN_VOLATILITY_DAYS = 5


@dataclass(frozen=True)
class WeightPlan:
    weights: dict[str, float]
    share: float
    volatility: float | None


def weights_for(picks: list[str], prices: pd.DataFrame, rule: Weights, top: int) -> WeightPlan:
    if not picks:
        return WeightPlan({}, 1.0, None)

    split = _inverse_volatility(picks, prices, rule, top) if rule.method == "inverseVolatility" else _equal(picks, top)
    capped = _capped(split, rule.max_per_fund)

    if rule.target_volatility is None:
        return WeightPlan(capped, 1.0, None)

    return _scaled_to_target(capped, prices, rule)


def _equal(picks: list[str], top: int) -> dict[str, float]:
    return {key: 1.0 / top for key in picks}


def _inverse_volatility(picks: list[str], prices: pd.DataFrame, rule: Weights, top: int) -> dict[str, float]:
    volatilities = {key: _volatility(prices, key, rule.volatility_days) for key in picks}
    inverse = {key: 1.0 / volatility for key, volatility in volatilities.items() if volatility}
    if not inverse:
        return _equal(picks, top)

    typical = float(np.mean(list(inverse.values())))
    raw = {key: inverse.get(key, typical) for key in picks}
    total = len(picks) / top
    scale = total / sum(raw.values())

    return {key: value * scale for key, value in raw.items()}


def _volatility(prices: pd.DataFrame, key: str, days: int) -> float | None:
    if key not in prices.columns:
        return None

    daily = prices[key].dropna().tail(days + 1).pct_change().dropna()
    if len(daily) < MIN_VOLATILITY_DAYS:
        return None

    return float(daily.std() * np.sqrt(TRADING_DAYS_PER_YEAR))


def _capped(weights: dict[str, float], cap: float | None) -> dict[str, float]:
    if cap is None:
        return weights

    return {key: min(weight, cap) for key, weight in weights.items()}


def _scaled_to_target(weights: dict[str, float], prices: pd.DataFrame, rule: Weights) -> WeightPlan:
    volatility = portfolio_volatility(prices, weights, rule.target_volatility_days)
    if volatility is None or volatility <= rule.target_volatility:
        return WeightPlan(weights, 1.0, volatility)

    share = rule.target_volatility / volatility
    scaled = {key: weight * share for key, weight in weights.items()}

    return WeightPlan(scaled, share, volatility)
