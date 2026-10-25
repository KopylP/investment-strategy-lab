"""Pydantic models of the v1 contract with the .NET backend.

JSON uses camelCase. Series are keyed by whatever the caller chooses (the backend sends
instrument ids); the engine never resolves tickers or downloads prices inside /decide.
"""

from __future__ import annotations

from datetime import date
from enum import Enum
from typing import Any

from pydantic import BaseModel, ConfigDict, Field, model_validator
from pydantic.alias_generators import to_camel


class ApiModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid")


class SeriesRole(str, Enum):
    INSTRUMENT = "instrument"
    BENCHMARK = "benchmark"
    VOLATILITY_INDEX = "volatilityIndex"


class PriceSeries(ApiModel):
    """Daily closes of one series, columnar. ``adjustedClose`` drives the signals."""

    key: str = Field(min_length=1)
    role: SeriesRole = SeriesRole.INSTRUMENT
    dates: list[date]
    adjusted_close: list[float | None]
    close: list[float | None] | None = None

    @model_validator(mode="after")
    def _aligned_and_ordered(self) -> "PriceSeries":
        if len(self.adjusted_close) != len(self.dates) or (self.close is not None and len(self.close) != len(self.dates)):
            raise ValueError(f"series '{self.key}': dates and values must have the same length")
        if any(later <= earlier for earlier, later in zip(self.dates, self.dates[1:])):
            raise ValueError(f"series '{self.key}': dates must be strictly ascending")
        return self


class UniverseMember(ApiModel):
    key: str = Field(min_length=1)
    name: str | None = None
    group: str | None = None


class Lot(ApiModel):
    quantity: float = Field(gt=0)
    price: float = Field(gt=0)
    acquired_on: date | None = None


class Holding(ApiModel):
    key: str = Field(min_length=1)
    lots: list[Lot] = Field(min_length=1)

    @property
    def quantity(self) -> float:
        return sum(lot.quantity for lot in self.lots)


class SubPortfolioSnapshot(ApiModel):
    currency: str = Field(min_length=3, max_length=3)
    cash: float = Field(default=0.0, ge=0)
    contribution: float = Field(default=0.0, ge=0)
    holdings: list[Holding] = Field(default_factory=list)


class EvaluationContext(ApiModel):
    """Everything a strategy may look at. Data after ``asOf`` is ignored."""

    as_of: date
    parameters: dict[str, Any] = Field(default_factory=dict)
    universe: list[UniverseMember] = Field(min_length=1)
    cash_instrument_key: str | None = None
    market_series: list[PriceSeries]
    sub_portfolio: SubPortfolioSnapshot
    state: dict[str, Any] | None = None

    @model_validator(mode="after")
    def _unique_keys(self) -> "EvaluationContext":
        keys = [series.key for series in self.market_series]
        if len(keys) != len(set(keys)):
            raise ValueError("marketSeries keys must be unique")
        return self


class TargetWeight(ApiModel):
    key: str
    weight: float


class MarketRegime(ApiModel):
    is_favorable: bool
    benchmark_price: float | None
    benchmark_moving_average: float | None
    volatility_index: float | None
    reasons: list[str]


class InstrumentScore(ApiModel):
    key: str
    name: str
    group: str
    price: float | None
    momentum: float | None
    return_1m: float | None
    return_3m: float | None
    return_12m: float | None
    volatility: float | None
    sharpe: float | None
    drawdown: float | None
    above_moving_average: bool
    last_date: date | None
    rank: int | None
    status: str
    reason: str
    weight: float | None


class Rationale(ApiModel):
    summary: str
    risk_free_rate: float
    scores: list[InstrumentScore]


class StrategyDecision(ApiModel):
    """Targets, not orders: the backend's rebalancer turns them into trades."""

    strategy_code: str
    engine_version: str
    as_of: date
    invest_fraction: float
    targets: list[TargetWeight]
    market: MarketRegime
    rationale: Rationale
    state: dict[str, Any] | None = None
    warnings: list[str] = Field(default_factory=list)


class RiskSignalKind(str, Enum):
    WARN = "warn"
    EXIT = "exit"


class RiskSignal(ApiModel):
    key: str
    kind: RiskSignalKind
    reason: str
    exit_price: float | None = None


class PositionRisk(ApiModel):
    key: str
    status: str
    quantity: float
    average_cost: float
    current_price: float
    value: float
    pnl_pct: float
    drawdown_from_high: float
    above_moving_average: bool
    moving_average: float | None
    exit_price: float | None
    exit_rule: str
    exit_distance: float | None
    stop_price: float | None
    ma_exit_price: float | None
    trail_exit_price: float | None
    alerts: list[str]
    sell: bool


class PortfolioRisk(ApiModel):
    value: float
    cost: float
    pnl_pct: float | None
    drawdown: float | None
    peak_date: date | None
    exit_limit_hit: bool
    alerts: list[str]


class RiskAssessment(ApiModel):
    strategy_code: str
    engine_version: str
    as_of: date
    status: str = Field(description="ok | watch | act")
    market_alerts: list[str]
    positions: list[PositionRisk]
    portfolio: PortfolioRisk
    signals: list[RiskSignal]
    actions: list[str]


class StrategyTypeDescriptor(ApiModel):
    code: str
    name: str
    description: str
    engine: str
    version: str
    supports_state: bool
    required_series_roles: list[SeriesRole]
    parameters_schema: dict[str, Any]
    default_parameters: dict[str, Any]


class PricesRequest(ApiModel):
    symbols: list[str] = Field(min_length=1)
    start: date
    end: date | None = None


class QuoteSeries(ApiModel):
    symbol: str
    dates: list[date]
    close: list[float | None]
    adjusted_close: list[float | None]


class PricesResponse(ApiModel):
    series: list[QuoteSeries]
    missing: list[str]


class SymbolQuery(ApiModel):
    """Yahoo symbols to try in order; the first one that exists (in ``currency``, when given) wins."""

    candidates: list[str] = Field(min_length=1)
    currency: str | None = None


class SymbolLookupRequest(ApiModel):
    queries: list[SymbolQuery] = Field(min_length=1)


class SymbolInfo(ApiModel):
    symbol: str
    name: str
    currency: str
    quote_type: str


class SymbolLookupResponse(ApiModel):
    """One entry per query, in order; ``null`` when no candidate exists."""

    results: list[SymbolInfo | None]
