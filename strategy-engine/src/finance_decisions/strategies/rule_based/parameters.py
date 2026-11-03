from __future__ import annotations

from typing import Annotated, Any, Literal, Union

from pydantic import BaseModel, ConfigDict, Discriminator, Field, StringConstraints, Tag
from pydantic.alias_generators import to_camel

from finance_decisions.messages import msg

INDICATOR_NAME_PATTERN = r"^[a-z][A-Za-z0-9]{0,39}$"
FRACTION = {"format": "fraction"}

IndicatorName = Annotated[str, StringConstraints(pattern=INDICATOR_NAME_PATTERN)]


def _vocabulary(group: str, kind: str, column_format: str | None = None) -> ConfigDict:
    extra: dict[str, Any] = {
        "x-label": msg(f"rule_based.{group}.{kind}.label"),
        "x-hint": msg(f"rule_based.{group}.{kind}.hint"),
        "x-phrase": msg(f"rule_based.{group}.{kind}.phrase"),
    }
    if column_format is not None:
        extra["x-column-format"] = column_format

    return ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid", frozen=True, json_schema_extra=extra)


def _choices(field: str, *values: str) -> dict[str, Any]:
    return {"x-choice-labels": {value: msg(f"rule_based.choice.{field}.{value}") for value in values}}


class _Rules(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid", frozen=True)


class SeriesRef(_Rules):
    series: Literal["fund", "benchmark", "volatilityIndex", "extra"] = Field(
        description=msg("rule_based.field.series"),
        json_schema_extra=_choices("series", "fund", "benchmark", "volatilityIndex", "extra"),
    )
    instrument_id: str | None = Field(None, description=msg("rule_based.field.instrumentId"))


def _of() -> Any:
    return Field(description=msg("rule_based.field.of"))


def _days(low: int, high: int) -> Any:
    return Field(ge=low, le=high, description=msg("rule_based.field.days"))


def _months(low: int, high: int) -> Any:
    return Field(ge=low, le=high, description=msg("rule_based.field.months"))


class PriceIndicator(_Rules):
    model_config = _vocabulary("indicator", "price", "number")
    kind: Literal["price"]
    of: SeriesRef = _of()


class SmaIndicator(_Rules):
    model_config = _vocabulary("indicator", "sma", "number")
    kind: Literal["sma"]
    of: SeriesRef = _of()
    days: int = _days(2, 400)


class EmaIndicator(_Rules):
    model_config = _vocabulary("indicator", "ema", "number")
    kind: Literal["ema"]
    of: SeriesRef = _of()
    days: int = _days(2, 400)


class PriceVsSmaIndicator(_Rules):
    model_config = _vocabulary("indicator", "priceVsSma", "signedPercent")
    kind: Literal["priceVsSma"]
    of: SeriesRef = _of()
    days: int = _days(2, 400)


class MomentumIndicator(_Rules):
    model_config = _vocabulary("indicator", "momentum", "signedPercent")
    kind: Literal["momentum"]
    of: SeriesRef = _of()
    months: int = _months(1, 24)


class BlendedMomentumIndicator(_Rules):
    model_config = _vocabulary("indicator", "blendedMomentum", "signedPercent")
    kind: Literal["blendedMomentum"]
    of: SeriesRef = _of()


class RelativeStrengthIndicator(_Rules):
    model_config = _vocabulary("indicator", "relativeStrength", "signedPercent")
    kind: Literal["relativeStrength"]
    of: SeriesRef = _of()
    months: int = _months(1, 24)


class VolatilityIndicator(_Rules):
    model_config = _vocabulary("indicator", "volatility", "percent")
    kind: Literal["volatility"]
    of: SeriesRef = _of()
    days: int = _days(5, 252)


class DrawdownIndicator(_Rules):
    model_config = _vocabulary("indicator", "drawdown", "signedPercent")
    kind: Literal["drawdown"]
    of: SeriesRef = _of()
    days: int = _days(5, 756)


class SharpeIndicator(_Rules):
    model_config = _vocabulary("indicator", "sharpe", "number")
    kind: Literal["sharpe"]
    of: SeriesRef = _of()
    months: int = _months(1, 36)


class RsiIndicator(_Rules):
    model_config = _vocabulary("indicator", "rsi", "number")
    kind: Literal["rsi"]
    of: SeriesRef = _of()
    days: int = _days(2, 100)


Indicator = Annotated[
    Union[
        PriceIndicator,
        SmaIndicator,
        EmaIndicator,
        PriceVsSmaIndicator,
        MomentumIndicator,
        BlendedMomentumIndicator,
        RelativeStrengthIndicator,
        VolatilityIndicator,
        DrawdownIndicator,
        SharpeIndicator,
        RsiIndicator,
    ],
    Field(discriminator="kind"),
]


class IndicatorOperand(_Rules):
    indicator: IndicatorName = Field(description=msg("rule_based.field.indicator"))


class ValueOperand(_Rules):
    value: float = Field(description=msg("rule_based.field.value"))


def _operand_kind(value: Any) -> str | None:
    if isinstance(value, dict):
        return "indicator" if "indicator" in value else "value" if "value" in value else None
    if isinstance(value, IndicatorOperand):
        return "indicator"
    if isinstance(value, ValueOperand):
        return "value"
    return None


Operand = Annotated[
    Union[Annotated[IndicatorOperand, Tag("indicator")], Annotated[ValueOperand, Tag("value")]],
    Discriminator(_operand_kind),
]


class CompareCondition(_Rules):
    model_config = _vocabulary("condition", "compare")
    kind: Literal["compare"]
    left: Operand = Field(description=msg("rule_based.field.left"))
    operator: Literal["gt", "lt", "gte", "lte"] = Field(
        description=msg("rule_based.field.operator"),
        json_schema_extra=_choices("operator", "gt", "lt", "gte", "lte"),
    )
    right: Operand = Field(description=msg("rule_based.field.right"))


class BetweenCondition(_Rules):
    model_config = _vocabulary("condition", "between")
    kind: Literal["between"]
    left: Operand = Field(description=msg("rule_based.field.left"))
    low: float = Field(description=msg("rule_based.field.low"))
    high: float = Field(description=msg("rule_based.field.high"))


class CrossedCondition(_Rules):
    model_config = _vocabulary("condition", "crossed")
    kind: Literal["crossed"]
    indicator: IndicatorName = Field(description=msg("rule_based.field.indicator"))
    direction: Literal["up", "down"] = Field(
        description=msg("rule_based.field.direction"),
        json_schema_extra=_choices("direction", "up", "down"),
    )
    level: float | None = Field(None, description=msg("rule_based.field.level"))
    other: IndicatorName | None = Field(None, description=msg("rule_based.field.other"))
    within_days: int = Field(21, ge=1, le=252, description=msg("rule_based.field.withinDays"))


class SignalCondition(_Rules):
    model_config = _vocabulary("condition", "signal")
    kind: Literal["signal"]
    signal_id: str = Field(description=msg("rule_based.field.signalId"))
    state: Literal["active", "inactive"] = Field(
        description=msg("rule_based.field.state"),
        json_schema_extra=_choices("state", "active", "inactive"),
    )
    scope: Literal["anyInstrument", "thisFund"] = Field(
        description=msg("rule_based.field.scope"),
        json_schema_extra=_choices("scope", "anyInstrument", "thisFund"),
    )


class AllCondition(_Rules):
    model_config = _vocabulary("condition", "all")
    kind: Literal["all"]
    conditions: list[Condition] = Field(min_length=1, description=msg("rule_based.field.conditions"))


class AnyCondition(_Rules):
    model_config = _vocabulary("condition", "any")
    kind: Literal["any"]
    conditions: list[Condition] = Field(min_length=1, description=msg("rule_based.field.conditions"))


class NotCondition(_Rules):
    model_config = _vocabulary("condition", "not")
    kind: Literal["not"]
    condition: Condition = Field(description=msg("rule_based.field.condition"))


Condition = Annotated[
    Union[CompareCondition, BetweenCondition, CrossedCondition, SignalCondition, AllCondition, AnyCondition, NotCondition],
    Field(discriminator="kind"),
]


class Gate(_Rules):
    condition: Condition = Field(description=msg("rule_based.field.condition"))


class MarketFilter(_Rules):
    condition: Condition = Field(description=msg("rule_based.field.condition"))
    invest_when_unfavorable: float = Field(ge=0, le=1, description=msg("rule_based.field.investWhenUnfavorable"), json_schema_extra=FRACTION)


class Selection(_Rules):
    rank_by: IndicatorName | None = Field(None, description=msg("rule_based.field.rankBy"))
    order: Literal["desc", "asc"] = Field("desc", description=msg("rule_based.field.order"), json_schema_extra=_choices("order", "desc", "asc"))
    top: int = Field(ge=1, le=60, description=msg("rule_based.field.top"))
    max_per_group: int | None = Field(None, ge=1, le=20, description=msg("rule_based.field.maxPerGroup"))
    max_correlation: float | None = Field(None, gt=0, le=1, description=msg("rule_based.field.maxCorrelation"), json_schema_extra=FRACTION)
    correlation_days: int = Field(126, ge=20, le=252, description=msg("rule_based.field.correlationDays"))


class Weights(_Rules):
    method: Literal["equal", "inverseVolatility"] = Field(
        description=msg("rule_based.field.method"),
        json_schema_extra=_choices("method", "equal", "inverseVolatility"),
    )
    volatility_days: int = Field(63, ge=5, le=252, description=msg("rule_based.field.volatilityDays"))
    max_per_fund: float | None = Field(None, gt=0, le=1, description=msg("rule_based.field.maxPerFund"), json_schema_extra=FRACTION)
    target_volatility: float | None = Field(None, ge=0.02, le=1, description=msg("rule_based.field.targetVolatility"), json_schema_extra=FRACTION)
    target_volatility_days: int = Field(21, ge=5, le=126, description=msg("rule_based.field.targetVolatilityDays"))


class Risk(_Rules):
    stop_loss_from_cost: float | None = Field(None, ge=-1, le=0, description=msg("rule_based.field.stopLossFromCost"), json_schema_extra=FRACTION)
    drawdown_from_peak: float | None = Field(None, ge=-1, le=0, description=msg("rule_based.field.drawdownFromPeak"), json_schema_extra=FRACTION)


class RuleParameters(_Rules):
    format_version: Literal[1] = Field(description=msg("rule_based.field.formatVersion"))
    indicators: dict[IndicatorName, Indicator] = Field(description=msg("rule_based.field.indicators"))
    market_filter: MarketFilter | None = Field(None, description=msg("rule_based.field.marketFilter"))
    exit: Gate | None = Field(None, description=msg("rule_based.field.exit"))
    entry: Gate | None = Field(None, description=msg("rule_based.field.entry"))
    selection: Selection = Field(description=msg("rule_based.field.selection"))
    weights: Weights = Field(description=msg("rule_based.field.weights"))
    risk: Risk | None = Field(None, description=msg("rule_based.field.risk"))


AllCondition.model_rebuild()
AnyCondition.model_rebuild()
NotCondition.model_rebuild()
Gate.model_rebuild()
MarketFilter.model_rebuild()
RuleParameters.model_rebuild()
