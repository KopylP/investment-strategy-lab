"""Shared helper functions used across risk and scanner modules."""

from __future__ import annotations

import pandas as pd

from finance_decisions.messages import msg, t


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


def last_valid_date(series: pd.Series) -> pd.Timestamp | None:
    """Return the index label of the last non-NaN value, or None."""
    clean = series.dropna()
    if clean.empty:
        return None
    return clean.index[-1]


def completed_month_closes(series: pd.Series, now: pd.Timestamp | None = None) -> pd.Series:
    """Return month-end closes for completed months only.

    The current month is dropped while it is still in progress: its last
    observation is before the month's final business day and *now* has not
    moved into a later month yet.
    """
    clean = series.dropna()
    if clean.empty:
        return clean
    monthly = clean.resample("ME").last()
    last = clean.index[-1]
    if now is not None and (now.year, now.month) > (last.year, last.month):
        return monthly
    if last.normalize() < (last + pd.offsets.BMonthEnd(0)).normalize():
        monthly = monthly.iloc[:-1]
    return monthly


def latest_rolling_high(series: pd.Series, period: int) -> float | None:
    """Return the rolling high over the last *period* non-NaN values."""
    clean = series.dropna()
    if clean.empty:
        return None
    return float(clean.rolling(period, min_periods=1).max().iloc[-1])


def holdings_by_value(holdings: dict[str, float], prices: dict[str, float | None]) -> list[str]:
    """Held tickers, largest position first."""
    return sorted(holdings, key=lambda ticker: holdings[ticker] * (prices.get(ticker) or 0.0), reverse=True)


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
    currency_upper = currency.upper()
    prefix = symbols.get(currency_upper, f"{currency_upper} ")
    sign = "-" if value < 0 else ""
    return f"{sign}{prefix}{abs(value):,.{decimals}f}"



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


def ua_month(value) -> str:
    return t("engine.month_year", month=msg(f"engine.month.{value.month}"), year=value.year)


def ua_day(value, weekday: bool = False) -> str:
    month = msg(f"engine.month_of.{value.month}")
    if weekday:
        return t("engine.weekday_day", weekday=msg(f"engine.weekday.{value.weekday()}"), day=value.day, month=month)
    return t("engine.day", day=value.day, month=month)


def next_weekday(value):
    """First Monday-to-Friday date strictly after *value*."""
    from datetime import timedelta

    day = value + timedelta(days=1)
    while day.weekday() >= 5:
        day += timedelta(days=1)
    return day
