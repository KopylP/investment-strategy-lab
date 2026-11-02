"""Strategy types the engine can run, and GET /v1/strategies."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Callable

from fastapi import APIRouter, HTTPException, status
from fastapi.encoders import jsonable_encoder
from pydantic import BaseModel, ValidationError

from finance_decisions.messages import msg
from finance_decisions.strategies.adaptive_momentum import decision as adaptive_momentum
from finance_decisions.strategies.adaptive_momentum.parameters import AdaptiveMomentumParameters
from finance_decisions.strategies.buy_and_hold import decision as buy_and_hold
from finance_decisions.strategies.buy_and_hold.parameters import BuyAndHoldParameters
from finance_decisions.strategies.etf_momentum import decision as etf_momentum
from finance_decisions.strategies.etf_momentum.parameters import EtfMomentumParameters
from .contracts import EvaluationContext, ReportDefinition, RiskAssessment, SeriesRole, StrategyDecision, StrategyTypeDescriptor

router = APIRouter(prefix="/v1", tags=["catalog"])


@dataclass(frozen=True)
class StrategyType:
    code: str
    name: str
    description: str
    messages: dict[str, str]
    parameters: type[BaseModel]
    decide: Callable[[EvaluationContext, Any], StrategyDecision]
    assess_risk: Callable[[EvaluationContext, Any], RiskAssessment]
    required_series_roles: tuple[SeriesRole, ...]
    reports: tuple[ReportDefinition, ...]
    # The engine version a decision of this type reports (strategy code / package version).
    version: Callable[[], str]
    supports_backtest: bool = True

    def descriptor(self) -> StrategyTypeDescriptor:
        return StrategyTypeDescriptor(
            code=self.code,
            name=self.name,
            description=self.description,
            engine="python",
            version=self.version(),
            supports_state=False,
            required_series_roles=list(self.required_series_roles),
            parameters_schema=self.parameters.model_json_schema(by_alias=True),
            default_parameters=self.parameters().model_dump(by_alias=True, mode="json"),
            reports=list(self.reports),
            supports_backtest=self.supports_backtest,
            messages=self.messages,
        )

    def parse_parameters(self, raw: dict[str, Any]) -> Any:
        try:
            return self.parameters.model_validate(raw)
        except ValidationError as error:
            detail = jsonable_encoder(error.errors(include_url=False, include_context=False))
            raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, detail=detail) from error


STRATEGY_TYPES: dict[str, StrategyType] = {
    etf_momentum.STRATEGY_CODE: StrategyType(
        code=etf_momentum.STRATEGY_CODE,
        name="ETF momentum",
        description=msg("etf_momentum.description"),
        messages=etf_momentum.MESSAGES,
        parameters=EtfMomentumParameters,
        decide=etf_momentum.decide,
        assess_risk=etf_momentum.assess_risk,
        required_series_roles=(SeriesRole.INSTRUMENT, SeriesRole.BENCHMARK, SeriesRole.VOLATILITY_INDEX),
        reports=etf_momentum.REPORTS,
        version=etf_momentum.engine_version,
    ),
    adaptive_momentum.STRATEGY_CODE: StrategyType(
        code=adaptive_momentum.STRATEGY_CODE,
        name="Adaptive momentum",
        description=msg("adaptive_momentum.description"),
        messages=adaptive_momentum.MESSAGES,
        parameters=AdaptiveMomentumParameters,
        decide=adaptive_momentum.decide,
        assess_risk=adaptive_momentum.assess_risk,
        required_series_roles=(SeriesRole.INSTRUMENT, SeriesRole.BENCHMARK),
        reports=adaptive_momentum.REPORTS,
        version=adaptive_momentum.engine_version,
    ),
    buy_and_hold.STRATEGY_CODE: StrategyType(
        code=buy_and_hold.STRATEGY_CODE,
        name="Buy and hold",
        description=msg("buy_and_hold.description"),
        messages=buy_and_hold.MESSAGES,
        parameters=BuyAndHoldParameters,
        decide=buy_and_hold.decide,
        assess_risk=buy_and_hold.assess_risk,
        required_series_roles=(SeriesRole.INSTRUMENT, SeriesRole.BENCHMARK),
        reports=buy_and_hold.REPORTS,
        version=buy_and_hold.engine_version,
    ),
}


def find_strategy_type(code: str) -> StrategyType:
    if code not in STRATEGY_TYPES:
        raise HTTPException(status.HTTP_404_NOT_FOUND, detail=f"Unknown strategy type '{code}'.")
    return STRATEGY_TYPES[code]


@router.get("/strategies", response_model=list[StrategyTypeDescriptor])
def list_strategy_types() -> list[StrategyTypeDescriptor]:
    return [strategy_type.descriptor() for strategy_type in STRATEGY_TYPES.values()]
