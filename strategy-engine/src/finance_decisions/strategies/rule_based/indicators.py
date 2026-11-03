from __future__ import annotations

from typing import Callable

import numpy as np
import pandas as pd

from finance_decisions.portfolio_math import TRADING_DAYS_PER_MONTH, TRADING_DAYS_PER_YEAR
from finance_decisions.strategies.errors import ContextError
from .parameters import Indicator

BLENDED_MOMENTUM_MONTHS = (3, 6, 12)
RSI_WHEN_FLAT = 50.0


def indicator_series(definition: Indicator, prices: pd.Series, benchmark: pd.Series | None) -> pd.Series:
    known = prices.dropna()
    calculate = _CALCULATIONS.get(definition.kind)
    if calculate is None:
        raise ValueError(f"unknown indicator kind '{definition.kind}'")

    values = calculate(definition, known, benchmark)

    return values.reindex(prices.index)


def history_needed(definition: Indicator) -> int:
    needed = _HISTORY.get(definition.kind)
    if needed is None:
        raise ValueError(f"unknown indicator kind '{definition.kind}'")

    return needed(definition)


def _price(_definition, prices: pd.Series, _benchmark) -> pd.Series:
    return prices


def _sma(definition, prices: pd.Series, _benchmark) -> pd.Series:
    return _moving_average(prices, definition.days)


def _ema(definition, prices: pd.Series, _benchmark) -> pd.Series:
    return prices.ewm(span=definition.days, adjust=False, min_periods=definition.days).mean()


def _price_vs_sma(definition, prices: pd.Series, _benchmark) -> pd.Series:
    return prices / _moving_average(prices, definition.days) - 1


def _momentum(definition, prices: pd.Series, _benchmark) -> pd.Series:
    return _trailing_return(prices, definition.months)


def _blended_momentum(_definition, prices: pd.Series, _benchmark) -> pd.Series:
    returns = [_trailing_return(prices, months) for months in BLENDED_MOMENTUM_MONTHS]

    return sum(returns) / len(returns)


def _relative_strength(definition, prices: pd.Series, benchmark: pd.Series | None) -> pd.Series:
    if benchmark is None:
        raise ContextError("no benchmark series for relative strength")

    benchmark_return = _trailing_return(benchmark.dropna(), definition.months)
    aligned = benchmark_return.reindex(prices.index, method="ffill")

    return _trailing_return(prices, definition.months) - aligned


def _volatility(definition, prices: pd.Series, _benchmark) -> pd.Series:
    daily = prices.pct_change()

    return daily.rolling(definition.days, min_periods=definition.days).std() * np.sqrt(TRADING_DAYS_PER_YEAR)


def _drawdown(definition, prices: pd.Series, _benchmark) -> pd.Series:
    peak = prices.rolling(definition.days, min_periods=definition.days).max()

    return prices / peak - 1


def _sharpe(definition, prices: pd.Series, _benchmark) -> pd.Series:
    window = definition.months * TRADING_DAYS_PER_MONTH
    daily = prices.pct_change()
    annual_return = daily.rolling(window, min_periods=window).mean() * TRADING_DAYS_PER_YEAR
    annual_volatility = daily.rolling(window, min_periods=window).std() * np.sqrt(TRADING_DAYS_PER_YEAR)

    return annual_return / annual_volatility


def _rsi(definition, prices: pd.Series, _benchmark) -> pd.Series:
    change = prices.diff()
    average_gain = _wilder_average(change.clip(lower=0), definition.days)
    average_loss = _wilder_average(-change.clip(upper=0), definition.days)
    rsi = 100 - 100 / (1 + average_gain / average_loss)

    flat = (average_gain == 0) & (average_loss == 0)

    return rsi.mask(flat, RSI_WHEN_FLAT)


def _wilder_average(values: pd.Series, days: int) -> pd.Series:
    return values.ewm(alpha=1 / days, adjust=False, min_periods=days).mean()


def _moving_average(prices: pd.Series, days: int) -> pd.Series:
    return prices.rolling(days, min_periods=days).mean()


def _trailing_return(prices: pd.Series, months: int) -> pd.Series:
    return prices / prices.shift(months * TRADING_DAYS_PER_MONTH) - 1


def _days_of_months(months: int) -> int:
    return months * TRADING_DAYS_PER_MONTH + 1


_CALCULATIONS: dict[str, Callable[..., pd.Series]] = {
    "price": _price,
    "sma": _sma,
    "ema": _ema,
    "priceVsSma": _price_vs_sma,
    "momentum": _momentum,
    "blendedMomentum": _blended_momentum,
    "relativeStrength": _relative_strength,
    "volatility": _volatility,
    "drawdown": _drawdown,
    "sharpe": _sharpe,
    "rsi": _rsi,
}

_HISTORY: dict[str, Callable[..., int]] = {
    "price": lambda _definition: 1,
    "sma": lambda definition: definition.days,
    "ema": lambda definition: definition.days,
    "priceVsSma": lambda definition: definition.days,
    "momentum": lambda definition: _days_of_months(definition.months),
    "blendedMomentum": lambda _definition: _days_of_months(max(BLENDED_MOMENTUM_MONTHS)),
    "relativeStrength": lambda definition: _days_of_months(definition.months),
    "volatility": lambda definition: definition.days + 1,
    "drawdown": lambda definition: definition.days,
    "sharpe": lambda definition: _days_of_months(definition.months),
    "rsi": lambda definition: definition.days + 1,
}
