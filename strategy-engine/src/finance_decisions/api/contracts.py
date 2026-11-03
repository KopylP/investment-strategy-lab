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

from finance_decisions.messages import msg


# Local wall-clock time, HH:mm.
TIME_PATTERN = r"^([01][0-9]|2[0-3]):[0-5][0-9]$"


class ApiModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid")


class SeriesRole(str, Enum):
    INSTRUMENT = "instrument"
    BENCHMARK = "benchmark"
    VOLATILITY_INDEX = "volatilityIndex"
    EXTRA = "extra"


class BacktestComparison(str, Enum):
    BENCHMARK = "benchmark"
    BENCHMARK_AT_EQUAL_RISK = "benchmark-at-equal-risk"
    UNIVERSE_EQUAL_WEIGHT = "universe-equal-weight"


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


class SignalState(ApiModel):
    id: str = Field(min_length=1)
    active_for: list[str] = Field(default_factory=list, description="Keys of the instruments (as in marketSeries) the signal fires for now.")


class EvaluationContext(ApiModel):
    """Everything a strategy may look at. Data after ``asOf`` is ignored."""

    as_of: date
    parameters: dict[str, Any] = Field(default_factory=dict)
    universe: list[UniverseMember] = Field(min_length=1)
    cash_instrument_key: str | None = None
    market_series: list[PriceSeries]
    sub_portfolio: SubPortfolioSnapshot
    state: dict[str, Any] | None = None
    signals: list[SignalState] | None = Field(None, description="States of the user's market signals the parameters refer to.")

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


class ColumnFormat(str, Enum):
    """How the report shows a value of a rationale column."""

    PERCENT = "percent"
    SIGNED_PERCENT = "signedPercent"
    NUMBER = "number"
    MONEY = "money"
    # A bool: an up arrow (true) or a down arrow (false).
    TREND = "trend"
    TEXT = "text"


class RationaleColumn(ApiModel):
    """One column of the "why" table; the report draws whatever columns the strategy lists, in this order.

    The id names the value in ``InstrumentScore.values``; it must not be ``key`` (the backend renames that one).
    """

    id: str
    label: str
    format: ColumnFormat
    # Header tooltip.
    hint: str | None = None
    # Shown on a phone too; the others only on a wider screen.
    compact: bool = False
    # Cell tooltips of a trend column.
    true_hint: str | None = None
    false_hint: str | None = None

    @model_validator(mode="after")
    def _id_is_not_key(self) -> "RationaleColumn":
        if self.id == "key":
            raise ValueError("a rationale column cannot be called 'key'")
        return self


class InstrumentScore(ApiModel):
    """One row of the "why" table. Only the identity, the decision and its reason are common to every strategy;
    the strategy's own metrics go into ``values`` under the ids of ``Rationale.columns``."""

    key: str
    name: str
    group: str
    status: str
    reason: str
    # Order of preference; the backend's order reasons quote it.
    rank: int | None = None
    # The strategy's main score as a fraction (momentum for etf-momentum); the backend's order reasons quote it too.
    momentum: float | None = None
    weight: float | None = None
    price: float | None = None
    values: dict[str, float | bool | str | None] = Field(default_factory=dict)
    # etf-momentum's metrics, kept for the backend's older reports; a new strategy leaves them out and fills values.
    return_1m: float | None = None
    return_3m: float | None = None
    return_12m: float | None = None
    volatility: float | None = None
    sharpe: float | None = None
    drawdown: float | None = None
    above_moving_average: bool | None = None
    last_date: date | None = None


class Rationale(ApiModel):
    summary: str
    scores: list[InstrumentScore]
    # Heading of the "why" table and of its instrument column.
    title: str = msg("rationale.default_title")
    instrument_label: str = msg("rationale.default_instrument")
    columns: list[RationaleColumn] = Field(default_factory=list)
    risk_free_rate: float | None = None


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


class ReportMode(str, Enum):
    """What a report does; the service knows how to make each."""

    # The contribution, buys and sells: what /decide says the sub-portfolio should hold.
    REBALANCE = "rebalance"
    # Sells only on an exit signal of /risk; everything else is a warning.
    RISK_REVIEW = "riskReview"


class ReportFrequency(str, Enum):
    OFF = "off"
    HOURLY = "hourly"
    DAILY = "daily"
    WEEKLY = "weekly"
    MONTHLY = "monthly"
    QUARTERLY = "quarterly"


class Weekday(str, Enum):
    MONDAY = "monday"
    TUESDAY = "tuesday"
    WEDNESDAY = "wednesday"
    THURSDAY = "thursday"
    FRIDAY = "friday"
    SATURDAY = "saturday"
    SUNDAY = "sunday"


class ReportSchedule(ApiModel):
    """When a report comes, in the service's time zone; each frequency reads only its own fields."""

    frequency: ReportFrequency
    day: int = Field(1, ge=1, le=31, description="Monthly: day of the month; quarterly: of the quarter's first month. A shorter month: its last day.")
    weekday: Weekday = Field(Weekday.SUNDAY, description="Weekly.")
    time: str = Field("20:00", pattern=TIME_PATTERN, description="Daily, weekly, monthly, quarterly: local time (HH:mm).")
    minute: int = Field(0, ge=0, le=59, description="Hourly: the minute of every hour.")


class SignalKind(str, Enum):
    """A kind of the service's built-in signals; a report may ask to come when one of them fires."""

    VOLATILITY_INDEX = "volatility-index"
    ACCOUNT_DRAWDOWN = "account-drawdown"
    POSITION_DRAWDOWN = "position-drawdown"


class ReportDefinition(ApiModel):
    """A report a strategy type makes: the service lists, schedules and generates it from this alone."""

    code: str = Field(pattern=r"^[a-z][a-z0-9-]*$")
    name: str
    description: str
    mode: ReportMode
    schedule: ReportSchedule = Field(description="When it comes unless the user sets their own time for the account.")
    signal_kinds: list[SignalKind] = Field(
        default_factory=list,
        description="Built-in signals that also bring this report, unless the user removes the link.",
    )


class StrategyTypeDescriptor(ApiModel):
    code: str
    name: str
    description: str
    engine: str
    version: str
    supports_state: bool
    required_series_roles: list[SeriesRole]
    parameters_schema: dict[str, Any]
    default_parameters: dict[str, Any] | None = Field(None, description="Null for a type whose parameters are always written in full, such as rules.")
    reports: list[ReportDefinition]
    supports_backtest: bool = Field(default=True, description="False for a strategy that cannot answer thousands of times in a row, such as an AI agent.")
    backtest_comparisons: list[BacktestComparison] = Field(
        default_factory=list,
        description="What a backtest of this type is shown next to, in this order: only the comparisons that answer a question about it.",
    )
    messages: dict[str, str] = Field(default_factory=dict, description="The texts of this type's message codes, in Ukrainian.")


class ParametersCheckRequest(ApiModel):
    parameters: dict[str, Any]


class ParameterProblem(ApiModel):
    path: list[str | int] = Field(description="JSON path of the problem inside the parameters.")
    code: str
    message: str


class ParametersCheck(ApiModel):
    problems: list[ParameterProblem]
    signal_ids: list[str] = Field(default_factory=list, description="User signals the parameters refer to, when their shape is valid.")


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
    description: str | None = None
    """What the company or fund is, as Yahoo writes it (English); ``null`` when Yahoo has none."""


class SymbolLookupResponse(ApiModel):
    """One entry per query, in order; ``null`` when no candidate exists."""

    results: list[SymbolInfo | None]


class JobStatus(str, Enum):
    RUNNING = "running"
    SUCCEEDED = "succeeded"
    FAILED = "failed"


class JobError(ApiModel):
    """Why a job failed, as the synchronous endpoint would have answered: 422 — the context was refused, 500 — the strategy broke."""

    status_code: int
    detail: Any


class StrategyJob(ApiModel):
    """A decision or risk assessment that runs in the background (a strategy may think for minutes or hours).

    ``result`` is what ``/decide`` or ``/risk`` returns, once the job has succeeded.
    """

    job_id: str
    code: str
    action: str
    status: JobStatus
    result: dict[str, Any] | None = None
    error: JobError | None = None


class FundScreenRequest(ApiModel):
    exchange: str | None = None
    region: str | None = None
    fund_family: str | None = None
    min_assets: float | None = Field(None, ge=0, allow_inf_nan=False)
    max_expense_ratio: float | None = Field(None, ge=0, le=0.1, allow_inf_nan=False)
    name_words: list[str] = Field(default_factory=list, max_length=5)
    size: int = Field(100, ge=1, le=250)

    @model_validator(mode="after")
    def _require_place(self) -> "FundScreenRequest":
        self.exchange = (self.exchange or "").strip() or None
        self.region = (self.region or "").strip() or None
        self.fund_family = (self.fund_family or "").strip() or None
        self.name_words = [word.strip() for word in self.name_words if word.strip()]
        if not self.exchange and not self.region:
            raise ValueError("a search needs an exchange or a region")
        return self


class ScreenedFund(ApiModel):
    symbol: str
    name: str
    currency: str | None = None
    exchange: str | None = None
    fund_family: str | None = None
    total_assets: float | None = None
    expense_ratio: float | None = None


class FundScreenResponse(ApiModel):
    funds: list[ScreenedFund]
