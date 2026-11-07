"""POST /v1/strategies/{code}/price-sets: prices of one replay, kept in memory and cut for each call."""

from __future__ import annotations

import threading
import time
import uuid
from bisect import bisect_left, bisect_right
from collections import OrderedDict
from datetime import date

from fastapi import APIRouter, HTTPException, status

from .catalog import find_strategy_type
from .contracts import EvaluationContext, PriceSeries, PriceSetCreated, PriceSetUpload

UNKNOWN_PRICE_SET = "price_set.unknown"
KEPT_FOR_SECONDS = 30 * 60
MOST_SETS = 16

router = APIRouter(prefix="/v1/strategies", tags=["strategies"])


class PriceSetStore:
    def __init__(self, kept_for_seconds: float = KEPT_FOR_SECONDS, most_sets: int = MOST_SETS) -> None:
        self._kept_for = kept_for_seconds
        self._most = most_sets
        self._sets: OrderedDict[str, tuple[float, list[PriceSeries]]] = OrderedDict()
        self._lock = threading.Lock()

    def add(self, series: list[PriceSeries]) -> str:
        set_id = uuid.uuid4().hex
        with self._lock:
            self._forget_stale()
            self._sets[set_id] = (time.monotonic(), series)
            while len(self._sets) > self._most:
                self._sets.popitem(last=False)
        return set_id

    def find(self, set_id: str) -> list[PriceSeries] | None:
        with self._lock:
            self._forget_stale()
            found = self._sets.get(set_id)
            if found is None:
                return None
            self._sets[set_id] = (time.monotonic(), found[1])
            self._sets.move_to_end(set_id)
            return found[1]

    def _forget_stale(self) -> None:
        oldest_kept = time.monotonic() - self._kept_for
        stale = [set_id for set_id, (used_at, _) in self._sets.items() if used_at < oldest_kept]
        for set_id in stale:
            del self._sets[set_id]


store = PriceSetStore()


@router.post("/{code}/price-sets", response_model=PriceSetCreated, status_code=status.HTTP_201_CREATED)
def upload_price_set(code: str, upload: PriceSetUpload) -> PriceSetCreated:
    find_strategy_type(code)

    return PriceSetCreated(id=store.add(upload.market_series))


def with_prices(context: EvaluationContext) -> EvaluationContext:
    if context.price_set_id is None:
        return context

    stored = store.find(context.price_set_id)
    if stored is None:
        raise HTTPException(status.HTTP_404_NOT_FOUND, detail=UNKNOWN_PRICE_SET)

    window = [_cut(series, context.history_from, context.as_of) for series in stored]

    return context.model_copy(update={"market_series": window, "price_set_id": None, "history_from": None})


def _cut(series: PriceSeries, first: date | None, last: date) -> PriceSeries:
    start = bisect_left(series.dates, first) if first is not None else 0
    end = bisect_right(series.dates, last)

    return series.model_copy(update={
        "dates": series.dates[start:end],
        "adjusted_close": series.adjusted_close[start:end],
        "close": _slice(series.close, start, end),
        "valuation_close": _slice(series.valuation_close, start, end),
    })


def _slice(values: list[float | None] | None, start: int, end: int) -> list[float | None] | None:
    return values[start:end] if values is not None else None
