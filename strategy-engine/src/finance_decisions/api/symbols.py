"""POST /v1/symbols/lookup: which Yahoo symbols exist, for instruments the backend has not registered yet."""

from __future__ import annotations

from typing import Annotated, Callable

from fastapi import APIRouter, Depends

from finance_decisions.market_data import symbol_info
from .contracts import SymbolInfo, SymbolLookupRequest, SymbolLookupResponse, SymbolQuery

router = APIRouter(prefix="/v1", tags=["symbols"])

SymbolInfoSource = Callable[[str], dict[str, str | None] | None]


def get_symbol_info() -> SymbolInfoSource:
    return symbol_info


@router.post("/symbols/lookup", response_model=SymbolLookupResponse)
def lookup(request: SymbolLookupRequest, source: Annotated[SymbolInfoSource, Depends(get_symbol_info)]) -> SymbolLookupResponse:
    return SymbolLookupResponse(results=[_first(query, source) for query in request.queries])


def _first(query: SymbolQuery, source: SymbolInfoSource) -> SymbolInfo | None:
    for candidate in dict.fromkeys(symbol.strip().upper() for symbol in query.candidates):
        info = source(candidate)
        if info is not None and (query.currency is None or str(info["currency"]).upper() == query.currency.upper()):
            return SymbolInfo(symbol=candidate, **info)
    return None
