"""Parameters of the etf-momentum strategy type as a validated, JSON-Schema-described model.

Defaults come from the dataclasses in config.py, so the two never drift apart.
"""

from __future__ import annotations

from pydantic import BaseModel, ConfigDict, Field, model_validator
from pydantic.alias_generators import to_camel

from finance_decisions.messages import msg
from .config import PlanSettings, RiskSettings, ScannerSettings


class _Parameters(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid", frozen=True)


class ScannerParameters(_Parameters):
    top_n: int = Field(ScannerSettings.top_n, ge=1, le=20, description=msg("etf_momentum.param.top_n"))
    lookback_months: int = Field(ScannerSettings.lookback_months, ge=1, le=24, description=msg("etf_momentum.param.lookback_months"))
    ma_period: int = Field(ScannerSettings.ma_period, ge=20, le=400, description=msg("etf_momentum.param.ma_period"))
    max_volatility: float = Field(ScannerSettings.max_volatility, gt=0, le=2, description=msg("etf_momentum.param.max_volatility"))
    max_drawdown: float = Field(ScannerSettings.max_drawdown, ge=-1, le=0, description=msg("etf_momentum.param.max_drawdown"))
    max_vix: float = Field(ScannerSettings.max_vix, gt=0, description=msg("etf_momentum.param.max_vix"))
    min_sharpe: float = Field(ScannerSettings.min_sharpe, description=msg("etf_momentum.param.min_sharpe"))
    min_momentum: float = Field(ScannerSettings.min_momentum, description=msg("etf_momentum.param.min_momentum"))
    hold_buffer_k: int = Field(ScannerSettings.hold_buffer_k, ge=1, le=50, description=msg("etf_momentum.param.hold_buffer_k"))
    max_per_group: int = Field(ScannerSettings.max_per_group, ge=1, le=20, description=msg("etf_momentum.param.max_per_group"))
    max_correlation: float = Field(ScannerSettings.max_correlation, gt=0, le=1, description=msg("etf_momentum.param.max_correlation"))
    risk_free_rate: float = Field(ScannerSettings.risk_free_rate, ge=-0.1, le=0.5, description=msg("etf_momentum.param.risk_free_rate"))
    max_stale_days: int = Field(ScannerSettings.max_stale_days, ge=1, le=30, description=msg("etf_momentum.param.max_stale_days"))

    @model_validator(mode="after")
    def _buffer_not_smaller_than_top_n(self) -> "ScannerParameters":
        if self.hold_buffer_k < self.top_n:
            raise ValueError("holdBufferK must be at least topN")
        return self

    def to_settings(self) -> ScannerSettings:
        return ScannerSettings(**self.model_dump())


class RiskParameters(_Parameters):
    ma_period: int = Field(RiskSettings.ma_period, ge=20, le=400)
    stop_loss_pct: float = Field(RiskSettings.stop_loss_pct, ge=-1, lt=0, description=msg("etf_momentum.param.stop_loss_pct"))
    drawdown_warn: float = Field(RiskSettings.drawdown_warn, ge=-1, lt=0)
    drawdown_exit: float = Field(RiskSettings.drawdown_exit, ge=-1, lt=0)
    vix_warn: float = Field(RiskSettings.vix_warn, gt=0)
    vix_exit: float = Field(RiskSettings.vix_exit, gt=0)
    portfolio_warn: float = Field(RiskSettings.portfolio_warn, ge=-1, lt=0)
    portfolio_exit: float = Field(RiskSettings.portfolio_exit, ge=-1, lt=0)
    ma_exit_band: float = Field(RiskSettings.ma_exit_band, ge=0, le=0.5, description=msg("etf_momentum.param.ma_exit_band"))
    small_loss_warn: float = Field(RiskSettings.small_loss_warn, ge=-1, le=0)
    vol_reference: float = Field(RiskSettings.vol_reference, gt=0, le=2)
    sell_on_position_exit: bool = Field(RiskSettings.sell_on_position_exit, description=msg("etf_momentum.param.sell_on_position_exit"))

    def to_settings(self) -> RiskSettings:
        return RiskSettings(**self.model_dump())


class EtfMomentumParameters(_Parameters):
    scanner: ScannerParameters = Field(default_factory=ScannerParameters)
    risk: RiskParameters = Field(default_factory=RiskParameters)
    weak_market_invest_pct: float = Field(PlanSettings.weak_market_invest_pct, ge=0, le=1, description=msg("etf_momentum.param.weak_market_invest_pct"))
