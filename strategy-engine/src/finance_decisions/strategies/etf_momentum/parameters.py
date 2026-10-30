"""Parameters of the etf-momentum strategy type as a validated, JSON-Schema-described model.

Defaults come from the dataclasses in config.py, so the two never drift apart.
"""

from __future__ import annotations

from pydantic import BaseModel, ConfigDict, Field, model_validator
from pydantic.alias_generators import to_camel

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


class EtfMomentumParameters(_Parameters):
    scanner: ScannerParameters = Field(default_factory=ScannerParameters)
    risk: RiskParameters = Field(default_factory=RiskParameters)
    weak_market_invest_pct: float = Field(PlanSettings.weak_market_invest_pct, ge=0, le=1, description="Частка внеску, яка інвестується, коли ринок слабкий.")
