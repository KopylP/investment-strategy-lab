"""Strategy types the engine can run, and GET /v1/strategies."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Callable

from fastapi import APIRouter, HTTPException, status
from fastapi.encoders import jsonable_encoder
from pydantic import BaseModel, ValidationError

from finance_decisions.strategies.etf_momentum import decision as etf_momentum
from finance_decisions.strategies.etf_momentum.parameters import EtfMomentumParameters
from .contracts import EvaluationContext, ReportDefinition, RiskAssessment, SeriesRole, StrategyDecision, StrategyTypeDescriptor

router = APIRouter(prefix="/v1", tags=["catalog"])


@dataclass(frozen=True)
class StrategyType:
    code: str
    name: str
    description: str
    parameters: type[BaseModel]
    decide: Callable[[EvaluationContext, Any], StrategyDecision]
    assess_risk: Callable[[EvaluationContext, Any], RiskAssessment]
    required_series_roles: tuple[SeriesRole, ...]
    reports: tuple[ReportDefinition, ...]
    # The engine version a decision of this type reports (strategy code / package version).
    version: Callable[[], str]

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
        description=(
            "Щомісяця обирає topN ETF з найсильнішим моментумом серед тих, що вище MA200, "
            "з помірною волатильністю й просадкою; у слабкому ринку інвестує лише частину внеску."
        ),
        parameters=EtfMomentumParameters,
        decide=etf_momentum.decide,
        assess_risk=etf_momentum.assess_risk,
        required_series_roles=(SeriesRole.INSTRUMENT, SeriesRole.BENCHMARK, SeriesRole.VOLATILITY_INDEX),
        reports=etf_momentum.REPORTS,
        version=etf_momentum.engine_version,
    )
}


def find_strategy_type(code: str) -> StrategyType:
    if code not in STRATEGY_TYPES:
        raise HTTPException(status.HTTP_404_NOT_FOUND, detail=f"Unknown strategy type '{code}'.")
    return STRATEGY_TYPES[code]


@router.get("/strategies", response_model=list[StrategyTypeDescriptor])
def list_strategy_types() -> list[StrategyTypeDescriptor]:
    return [strategy_type.descriptor() for strategy_type in STRATEGY_TYPES.values()]
