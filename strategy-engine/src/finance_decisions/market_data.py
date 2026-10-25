from __future__ import annotations

from datetime import date, timedelta

import pandas as pd
import yfinance as yf


def download_close_prices(tickers: list[str], period: str) -> pd.DataFrame:
    if not tickers:
        return pd.DataFrame()

    raw = yf.download(
        tickers,
        period=period,
        auto_adjust=True,
        progress=False,
        threads=False,
    )

    if raw.empty:
        return pd.DataFrame()

    if isinstance(raw.columns, pd.MultiIndex):
        prices = raw["Close"]
    else:
        prices = raw[["Close"]].rename(columns={"Close": tickers[0]})

    return prices.dropna(how="all")



def download_quotes(symbols: list[str], start: date, end: date | None = None) -> dict[str, pd.DataFrame]:
    """Daily ``close`` and ``adjusted_close`` per symbol from ``start`` to ``end`` (inclusive).

    Symbols without data are left out of the result.
    """
    if not symbols:
        return {}

    raw = yf.download(
        symbols,
        start=start.isoformat(),
        end=(end + timedelta(days=1)).isoformat() if end else None,
        auto_adjust=False,
        actions=False,
        progress=False,
        threads=False,
        group_by="column",
    )
    if raw.empty:
        return {}

    result = {}
    for symbol in symbols:
        close = _column(raw, "Close", symbol, len(symbols))
        adjusted = _column(raw, "Adj Close", symbol, len(symbols))
        if close is None or adjusted is None:
            continue
        frame = pd.DataFrame({"close": close, "adjusted_close": adjusted}).dropna(how="all")
        if not frame.empty:
            frame.index = pd.DatetimeIndex(frame.index).tz_localize(None).normalize()
            result[symbol] = frame
    return result


def _column(raw: pd.DataFrame, field: str, symbol: str, symbol_count: int) -> pd.Series | None:
    if isinstance(raw.columns, pd.MultiIndex):
        return raw[field][symbol] if (field, symbol) in raw.columns else None
    if symbol_count == 1 and field in raw.columns:
        return raw[field]
    return None
