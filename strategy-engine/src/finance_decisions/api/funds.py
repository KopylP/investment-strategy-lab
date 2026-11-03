from __future__ import annotations

import logging
from typing import Annotated, Callable

from fastapi import APIRouter, Depends, HTTPException

from finance_decisions.market_data import screen_funds, select_screened_funds
from .contracts import FundScreenRequest, FundScreenResponse, ScreenedFund

router = APIRouter(prefix="/v1/funds", tags=["funds"])
logger = logging.getLogger(__name__)
FundScreener = Callable[[FundScreenRequest], list[dict]]


def get_fund_screener() -> FundScreener:
    return screen_funds


@router.post("/screen", response_model=FundScreenResponse)
def screen(request: FundScreenRequest, source: Annotated[FundScreener, Depends(get_fund_screener)]) -> FundScreenResponse:
    try:
        matches = source(request)
        selected = select_screened_funds(matches, request)
        funds = [ScreenedFund(**match) for match in selected]
        return FundScreenResponse(funds=funds)
    except Exception as error:
        logger.warning("Fund screen failed", exc_info=True)
        raise HTTPException(status_code=502, detail="fund screen failed") from error
