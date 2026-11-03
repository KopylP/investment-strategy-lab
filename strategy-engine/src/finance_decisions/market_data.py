from __future__ import annotations

from datetime import date, timedelta
import math
import unicodedata
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from finance_decisions.api.contracts import FundScreenRequest

import pandas as pd
import yfinance as yf

BENCHMARK_SYMBOL = "SPY"
VOLATILITY_INDEX_SYMBOL = "^VIX"
MARKET_SYMBOLS = (BENCHMARK_SYMBOL, VOLATILITY_INDEX_SYMBOL)


def ticker_currency(ticker: str) -> str:
    """Determine the default trading currency based on the ticker suffix."""
    ticker_upper = ticker.upper()
    if "." in ticker_upper:
        suffix = ticker_upper.split(".")[-1]
        # Common European exchange suffixes trading in EUR
        if suffix in {"DE", "PA", "MI", "AS", "MC", "AT", "BR", "LI"}:
            return "EUR"
        elif suffix == "L":
            return "GBP"
        elif suffix == "SW":
            return "CHF"
    return "USD"


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


def symbol_info(symbol: str) -> dict[str, str | None] | None:
    """Name, currency, quote type (EQUITY, ETF, ...) and Yahoo's description of a Yahoo symbol, or None when Yahoo does not know it."""
    try:
        info = yf.Ticker(symbol).info or {}
    except Exception:
        return None
    if not info.get("quoteType") or not info.get("currency"):
        return None
    return {
        "name": info.get("longName") or info.get("shortName") or symbol,
        "currency": str(info["currency"]),
        "quote_type": str(info["quoteType"]),
        # As Yahoo writes it (English); funds listed in Europe often have none.
        "description": (str(info.get("longBusinessSummary") or "").strip() or None),
    }



def screen_funds(request: FundScreenRequest) -> list[dict]:
    query = _fund_screen_query(request)
    response = yf.screen(query, size=250, sortField="fundnetassets", sortAsc=False)
    if not isinstance(response, dict) or not isinstance(response.get("quotes"), list):
        raise ValueError("invalid fund screen response")

    matches = [_screened_fund(quote) for quote in response["quotes"]]
    return select_screened_funds(matches, request)


def _fund_screen_query(request: FundScreenRequest) -> yf.ETFQuery:
    if not request.exchange and not request.region:
        raise ValueError("a search needs an exchange or a region")

    filters = []
    criteria = [
        ("eq", "exchange", request.exchange),
        ("eq", "region", request.region),
        ("eq", "fundfamilyname", request.fund_family),
        ("gte", "fundnetassets", request.min_assets),
        ("lte", "annualreportnetexpenseratio", request.max_expense_ratio),
    ]
    for operator, field, value in criteria:
        if value is not None:
            filters.append(yf.ETFQuery(operator, [field, value]))

    return filters[0] if len(filters) == 1 else yf.ETFQuery("and", filters)


def _screened_fund(quote: dict) -> dict:
    symbol = quote["symbol"]
    return {
        "symbol": symbol,
        "name": quote.get("longName") or quote.get("shortName") or symbol,
        "currency": quote.get("currency"),
        "exchange": quote.get("fullExchangeName") or quote.get("exchange"),
        "fund_family": quote.get("fundFamily") or quote.get("fundfamilyname"),
        "total_assets": _finite_figure(quote.get("fundnetassets", quote.get("totalAssets"))),
        "expense_ratio": _finite_figure(quote.get("annualreportnetexpenseratio", quote.get("annualReportExpenseRatio"))),
    }


def _finite_figure(value) -> float | None:
    if value is None:
        return None
    number = float(value)
    return number if math.isfinite(number) else None


def select_screened_funds(matches: list[dict], request: FundScreenRequest) -> list[dict]:
    words = [_fold_fund_name(word) for word in request.name_words]
    selected = [match for match in matches if _fund_matches(match, request, words)]
    selected.sort(key=lambda match: match["total_assets"] if match["total_assets"] is not None else -1, reverse=True)
    return selected[:request.size]


def _fund_matches(match: dict, request: FundScreenRequest, words: list[str]) -> bool:
    assets = match["total_assets"]
    expense = match["expense_ratio"]
    if request.min_assets is not None and (assets is None or assets < request.min_assets):
        return False
    if request.max_expense_ratio is not None and (expense is None or expense > request.max_expense_ratio):
        return False

    name = _fold_fund_name(match["name"])
    return all(word in name for word in words)


def _fold_fund_name(value: str) -> str:
    decomposed = unicodedata.normalize("NFKD", value.casefold())
    return "".join(character for character in decomposed if not unicodedata.combining(character))
