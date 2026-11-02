from __future__ import annotations

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel

from finance_decisions.messages import msg
from .selection import SelectionRules


class _Parameters(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid", frozen=True)


class ScannerParameters(_Parameters):
    top_n: int = Field(3, ge=1, le=10, description=msg("adaptive_momentum.param.top_n"))
    trend_days: int = Field(200, ge=20, le=400, description=msg("adaptive_momentum.param.trend_days"))
    max_correlation: float = Field(0.7, gt=0, le=1, description=msg("adaptive_momentum.param.max_correlation"))
    max_per_group: int = Field(1, ge=1, le=10, description=msg("adaptive_momentum.param.max_per_group"))

    def to_rules(self) -> SelectionRules:
        return SelectionRules(
            top_n=self.top_n,
            trend_days=self.trend_days,
            max_correlation=self.max_correlation,
            max_per_group=self.max_per_group,
        )


class AdaptiveMomentumParameters(_Parameters):
    scanner: ScannerParameters = Field(default_factory=ScannerParameters)
    target_volatility: float = Field(0.14, ge=0.02, le=1, description=msg("adaptive_momentum.param.target_volatility"))
    volatility_window_days: int = Field(21, ge=5, le=126, description=msg("adaptive_momentum.param.volatility_window_days"))
