"""POST /v1/prices: daily open, high, low and close quotes from Yahoo Finance for the backend's MarketData module."""

from __future__ import annotations

from datetime import date
from typing import Annotated, Callable

import pandas as pd
from fastapi import APIRouter, Depends

from finance_decisions.market_data import download_quotes
from .contracts import PricesRequest, PricesResponse, QuoteSeries

router = APIRouter(prefix="/v1", tags=["prices"])

QuoteDownloader = Callable[[list[str], date, date | None], dict[str, pd.DataFrame]]


def get_quote_downloader() -> QuoteDownloader:
    return download_quotes


@router.post("/prices", response_model=PricesResponse)
def prices(request: PricesRequest, downloader: Annotated[QuoteDownloader, Depends(get_quote_downloader)]) -> PricesResponse:
    symbols = list(dict.fromkeys(request.symbols))
    frames = downloader(symbols, request.start, request.end)
    series = [
        QuoteSeries(
            symbol=symbol,
            dates=[timestamp.date() for timestamp in frames[symbol].index],
            open=_values(frames[symbol]["open"]),
            high=_values(frames[symbol]["high"]),
            low=_values(frames[symbol]["low"]),
            close=_values(frames[symbol]["close"]),
            adjusted_close=_values(frames[symbol]["adjusted_close"]),
        )
        for symbol in symbols
        if symbol in frames
    ]
    return PricesResponse(series=series, missing=[symbol for symbol in symbols if symbol not in frames])


def _values(column: pd.Series) -> list[float | None]:
    return [None if pd.isna(value) else float(value) for value in column]
