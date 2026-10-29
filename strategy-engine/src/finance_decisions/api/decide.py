"""POST /v1/strategies/{code}/decide: targets for one sub-portfolio."""

from __future__ import annotations

from fastapi import APIRouter, HTTPException, status

from finance_decisions.strategies.errors import ContextError
from .catalog import find_strategy_type
from .contracts import EvaluationContext, StrategyDecision

router = APIRouter(prefix="/v1/strategies", tags=["strategies"])


@router.post("/{code}/decide", response_model=StrategyDecision)
def decide(code: str, context: EvaluationContext) -> StrategyDecision:
    strategy_type = find_strategy_type(code)
    parameters = strategy_type.parse_parameters(context.parameters)
    try:
        return strategy_type.decide(context, parameters)
    except ContextError as error:
        raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, detail=str(error)) from error
