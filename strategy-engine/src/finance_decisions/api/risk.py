"""POST /v1/strategies/{code}/risk: weekly exit checks for one sub-portfolio."""

from __future__ import annotations

from fastapi import APIRouter, HTTPException, status

from finance_decisions.strategies.etf_momentum.decision import ContextError
from .catalog import find_strategy_type
from .contracts import EvaluationContext, RiskAssessment

router = APIRouter(prefix="/v1/strategies", tags=["strategies"])


@router.post("/{code}/risk", response_model=RiskAssessment)
def assess_risk(code: str, context: EvaluationContext) -> RiskAssessment:
    strategy_type = find_strategy_type(code)
    parameters = strategy_type.parse_parameters(context.parameters)
    try:
        return strategy_type.assess_risk(context, parameters)
    except ContextError as error:
        raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, detail=str(error)) from error
