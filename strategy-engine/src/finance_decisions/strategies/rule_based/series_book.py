from __future__ import annotations

from dataclasses import dataclass

import pandas as pd

from finance_decisions.api.contracts import EvaluationContext, PriceSeries, SeriesRole
from finance_decisions.strategies.errors import ContextError
from .parameters import SeriesRef

EXTRA_SEARCH_ROLES = (SeriesRole.EXTRA, SeriesRole.INSTRUMENT)


@dataclass(frozen=True)
class SeriesBook:
    by_role: dict[SeriesRole, dict[str, pd.Series]]

    @classmethod
    def from_context(cls, context: EvaluationContext) -> SeriesBook:
        by_role: dict[SeriesRole, dict[str, pd.Series]] = {role: {} for role in SeriesRole}
        for series in context.market_series:
            by_role[series.role][series.key] = _until(series, context)

        return cls(by_role)

    def fund(self, key: str) -> pd.Series:
        return self.by_role[SeriesRole.INSTRUMENT].get(key, pd.Series(dtype=float))

    def benchmark(self) -> pd.Series:
        return self._first_of(SeriesRole.BENCHMARK)

    def benchmark_or_none(self) -> pd.Series | None:
        found = self.by_role[SeriesRole.BENCHMARK]

        return next(iter(found.values())).dropna() if found else None

    def volatility_index(self) -> pd.Series:
        return self._first_of(SeriesRole.VOLATILITY_INDEX)

    def extra(self, instrument_id: str) -> pd.Series:
        for role in EXTRA_SEARCH_ROLES:
            if instrument_id in self.by_role[role]:
                return self.by_role[role][instrument_id]

        raise ContextError(f"no extra series for instrument {instrument_id}")

    def of(self, ref: SeriesRef, fund_key: str | None) -> pd.Series:
        if ref.series == "fund":
            if fund_key is None:
                raise ValueError("the fund series is read only for a fund")
            return self.fund(fund_key)
        if ref.series == "benchmark":
            return self.benchmark()
        if ref.series == "volatilityIndex":
            return self.volatility_index()
        if ref.instrument_id is None:
            raise ContextError("an extra series without its instrument")

        return self.extra(ref.instrument_id)

    def funds_frame(self) -> pd.DataFrame:
        frame = pd.DataFrame(self.by_role[SeriesRole.INSTRUMENT])
        if frame.empty:
            return frame

        return frame.sort_index().dropna(how="all")

    def _first_of(self, role: SeriesRole) -> pd.Series:
        found = self.by_role[role]
        if not found:
            raise ContextError(f"no {role.value} series")

        return next(iter(found.values()))


def _until(series: PriceSeries, context: EvaluationContext) -> pd.Series:
    prices = pd.Series(series.adjusted_close, index=pd.to_datetime(series.dates), dtype=float).sort_index()

    return prices.loc[: pd.Timestamp(context.as_of)]
