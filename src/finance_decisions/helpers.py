"""Shared helper functions used across risk and scanner modules."""

from __future__ import annotations

import pandas as pd


def latest_value(series: pd.Series) -> float | None:
    """Return the last non-NaN value in a Series, or None if all NaN/empty."""
    clean = series.dropna()
    if clean.empty:
        return None
    return float(clean.iloc[-1])


def latest_moving_average(series: pd.Series, period: int) -> float | None:
    """Return the moving average of the last *period* non-NaN values, or None
    if there are fewer than *period* clean data points."""
    clean = series.dropna()
    if len(clean) < period:
        return None
    return float(clean.rolling(period).mean().iloc[-1])


def latest_rolling_high(series: pd.Series, period: int) -> float | None:
    """Return the rolling high over the last *period* non-NaN values."""
    clean = series.dropna()
    if clean.empty:
        return None
    return float(clean.rolling(period, min_periods=1).max().iloc[-1])


def safe_division(numerator: float, denominator: float, default: float = 0.0) -> float:
    """Divide *numerator* by *denominator*, returning *default* when the
    denominator is zero or very close to zero."""
    if denominator == 0 or abs(denominator) < 1e-12:
        return default
    return numerator / denominator


def money(value: float, currency: str, decimals: int = 2) -> str:
    """Format a monetary value with a currency symbol prefix."""
    symbols = {
        "EUR": "€",
        "USD": "$",
        "GBP": "£",
        "PLN": "zł ",
    }
    prefix = symbols.get(currency, f"{currency} ")
    return f"{prefix}{value:,.{decimals}f}"


def is_na(value: object) -> bool:
    """Return True when *value* looks like a missing/NaN value.

    Unlike bare ``pd.isna()``, this never raises on array-like or
    unexpected types — it simply returns False for anything it cannot
    check.
    """
    if value is None:
        return True
    try:
        return bool(pd.isna(value))
    except (ValueError, TypeError):
        return False
