"""Parameters of the etf-momentum strategy type as a validated, JSON-Schema-described model.

Defaults come from the dataclasses in config.py, so the two never drift apart.
"""

from __future__ import annotations

from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator
from pydantic.alias_generators import to_camel

from finance_decisions.api.contracts import TIME_PATTERN, ReportSchedule, Weekday
from .config import PlanSettings, RiskSettings, ScannerSettings


class _Parameters(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid", frozen=True)


class ScannerParameters(_Parameters):
    top_n: int = Field(ScannerSettings.top_n, ge=1, le=20, description="Скільки ETF тримати; кожен отримує 1/topN портфеля.")
    lookback_months: int = Field(ScannerSettings.lookback_months, ge=1, le=24, description="Період моментуму в завершених місяцях.")
    ma_period: int = Field(ScannerSettings.ma_period, ge=20, le=400, description="Ковзна середня для фільтра тренду.")
    max_volatility: float = Field(ScannerSettings.max_volatility, gt=0, le=2, description="Максимальна річна волатильність.")
    max_drawdown: float = Field(ScannerSettings.max_drawdown, ge=-1, le=0, description="Найглибша допустима просадка за рік.")
    max_vix: float = Field(ScannerSettings.max_vix, gt=0, description="VIX, вище якого ринок вважається слабким.")
    min_sharpe: float = Field(ScannerSettings.min_sharpe, description="Мінімальний коефіцієнт Шарпа.")
    min_momentum: float = Field(ScannerSettings.min_momentum, description="Абсолютний моментум, який треба перевищити.")
    hold_buffer_k: int = Field(ScannerSettings.hold_buffer_k, ge=1, le=50, description="Утримувана позиція лишається, поки її ранг не гірший за це число.")
    max_per_group: int = Field(ScannerSettings.max_per_group, ge=1, le=20, description="Скільки ETF можна взяти з однієї групи.")
    risk_free_rate: float = Field(ScannerSettings.risk_free_rate, ge=-0.1, le=0.5, description="Безризикова ставка, якщо кеш-ETF має замало історії.")
    max_stale_days: int = Field(ScannerSettings.max_stale_days, ge=1, le=30, description="Скільки днів ціна може не оновлюватись.")

    @model_validator(mode="after")
    def _buffer_not_smaller_than_top_n(self) -> "ScannerParameters":
        if self.hold_buffer_k < self.top_n:
            raise ValueError("holdBufferK must be at least topN")
        return self

    def to_settings(self) -> ScannerSettings:
        return ScannerSettings(**self.model_dump())


class RiskParameters(_Parameters):
    ma_period: int = Field(RiskSettings.ma_period, ge=20, le=400)
    stop_loss_pct: float = Field(RiskSettings.stop_loss_pct, ge=-1, lt=0, description="Стоп-лосс від ціни входу (масштабується волатильністю).")
    drawdown_warn: float = Field(RiskSettings.drawdown_warn, ge=-1, lt=0)
    drawdown_exit: float = Field(RiskSettings.drawdown_exit, ge=-1, lt=0)
    vix_warn: float = Field(RiskSettings.vix_warn, gt=0)
    vix_exit: float = Field(RiskSettings.vix_exit, gt=0)
    portfolio_warn: float = Field(RiskSettings.portfolio_warn, ge=-1, lt=0)
    portfolio_exit: float = Field(RiskSettings.portfolio_exit, ge=-1, lt=0)
    ma_exit_band: float = Field(RiskSettings.ma_exit_band, ge=0, le=0.5, description="Продаж лише нижче MA200 мінус ця частка.")
    small_loss_warn: float = Field(RiskSettings.small_loss_warn, ge=-1, le=0)
    vol_reference: float = Field(RiskSettings.vol_reference, gt=0, le=2)

    def to_settings(self) -> RiskSettings:
        return RiskSettings(**self.model_dump())


_FREQUENCY_LABELS = {
    "off": "Вимкнено",
    "hourly": "Щогодини",
    "daily": "Щодня",
    "weekly": "Щотижня",
    "monthly": "Щомісяця",
    "quarterly": "Щокварталу",
}
_WEEKDAY_LABELS = {
    "monday": "Понеділок",
    "tuesday": "Вівторок",
    "wednesday": "Середа",
    "thursday": "Четвер",
    "friday": "Пʼятниця",
    "saturday": "Субота",
    "sunday": "Неділя",
}


def _shown_for(*frequencies: str) -> dict[str, Any]:
    """The form shows a field only while the schedule's frequency is one of these."""
    return {"x-showWhen": {"frequency": list(frequencies)}}


class _ScheduleParameters(_Parameters):
    """When one report comes (contracts.ReportSchedule); each frequency reads only its own fields."""

    frequency: str
    day: int = Field(1, ge=1, le=31, title="День місяця", description="У коротшому місяці — його останній день; щокварталу — день першого місяця кварталу.",
                     json_schema_extra=_shown_for("monthly", "quarterly"))
    weekday: Weekday = Field(Weekday.SUNDAY, title="День тижня",
                             json_schema_extra={"x-enumLabels": _WEEKDAY_LABELS, **_shown_for("weekly")})
    time: str = Field("20:00", pattern=TIME_PATTERN, title="Час", description="ГГ:ХХ, час сервісу.",
                      json_schema_extra=_shown_for("daily", "weekly", "monthly", "quarterly"))
    minute: int = Field(0, ge=0, le=59, title="Хвилина", description="Кожної години о цій хвилині.", json_schema_extra=_shown_for("hourly"))

    def to_schedule(self) -> ReportSchedule:
        return ReportSchedule.model_validate(self.model_dump())


class RebalanceSchedule(_ScheduleParameters):
    frequency: Literal["hourly", "daily", "weekly", "monthly", "quarterly"] = Field(
        "monthly", title="Як часто", json_schema_extra={"x-enumLabels": {key: _FREQUENCY_LABELS[key] for key in ("hourly", "daily", "weekly", "monthly", "quarterly")}}
    )


class RiskReviewSchedule(_ScheduleParameters):
    frequency: Literal["off", "hourly", "daily", "weekly"] = Field(
        "weekly", title="Як часто", json_schema_extra={"x-enumLabels": {key: _FREQUENCY_LABELS[key] for key in ("off", "hourly", "daily", "weekly")}}
    )
    time: str = Field("19:00", pattern=TIME_PATTERN, title="Час", description="ГГ:ХХ, час сервісу.",
                      json_schema_extra=_shown_for("daily", "weekly"))


class ReportingParameters(_Parameters):
    """When the strategy's reports (decision.REPORTS) come."""

    rebalance: RebalanceSchedule = Field(default_factory=RebalanceSchedule, title="Рапорт «Ребаланс»",
                                         description="Внесок, купівлі й продажі за рішенням стратегії.")
    risk_review: RiskReviewSchedule = Field(default_factory=RiskReviewSchedule, title="Рапорт «Перевірка ризику»",
                                            description="Між ребалансами: стоп-лоси, просідання, VIX — продаж лише за сигналом виходу.")


class EtfMomentumParameters(_Parameters):
    reporting: ReportingParameters = Field(default_factory=ReportingParameters, title="Рапорти")
    scanner: ScannerParameters = Field(default_factory=ScannerParameters)
    risk: RiskParameters = Field(default_factory=RiskParameters)
    weak_market_invest_pct: float = Field(PlanSettings.weak_market_invest_pct, ge=0, le=1, description="Частка внеску, яка інвестується, коли ринок слабкий.")
