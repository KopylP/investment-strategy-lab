from __future__ import annotations

from fastapi import APIRouter

from .catalog import find_strategy_type
from .contracts import ParametersCheck, ParametersCheckRequest

router = APIRouter(prefix="/v1/strategies", tags=["strategies"])


@router.post("/{code}/parameters/check", response_model=ParametersCheck)
def check_parameters(code: str, request: ParametersCheckRequest) -> ParametersCheck:
    strategy_type = find_strategy_type(code)

    return strategy_type.check(request.parameters)
