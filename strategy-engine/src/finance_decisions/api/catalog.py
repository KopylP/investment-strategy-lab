"""Strategy types the engine can run, and GET /v1/strategies."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Callable

from fastapi import APIRouter, HTTPException, status
from fastapi.encoders import jsonable_encoder
from pydantic import BaseModel, ValidationError

from pathlib import Path

from finance_decisions.messages import catalog_of, msg
from finance_decisions.strategies.adaptive_momentum import decision as adaptive_momentum
from finance_decisions.strategies.adaptive_momentum.parameters import AdaptiveMomentumParameters
from finance_decisions.strategies.buy_and_hold import decision as buy_and_hold
from finance_decisions.strategies.buy_and_hold.parameters import BuyAndHoldParameters
from finance_decisions.strategies.etf_momentum import decision as etf_momentum
from finance_decisions.strategies.etf_momentum.parameters import EtfMomentumParameters
from finance_decisions.strategies.rule_based import decision as rule_based
from finance_decisions.strategies.rule_based import risk as rule_based_risk
from finance_decisions.strategies.rule_based.checks import RuleProblem, problems_in, referenced_signals
from finance_decisions.strategies.rule_based.parameters import RuleParameters
from .contracts import (
    BacktestComparison,
    EvaluationContext,
    ParametersCheck,
    ReportDefinition,
    RiskAssessment,
    SeriesRole,
    StrategyDecision,
    StrategyTypeDescriptor,
)
from .parameter_problems import meaning_problem, shape_problems

router = APIRouter(prefix="/v1", tags=["catalog"])

PARAMETER_PROBLEM_PREFIX = "engine.parameters."
PARAMETER_PROBLEM_TEXTS = {
    code: text for code, text in catalog_of(Path(__file__).resolve().parents[1]).items() if code.startswith(PARAMETER_PROBLEM_PREFIX)
}


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
    backtest_comparisons: tuple[BacktestComparison, ...]
    supports_backtest: bool = True
    has_defaults: bool = True
    semantic_check: Callable[[Any], list[RuleProblem]] = lambda _parameters: []
    referenced_signals: Callable[[Any], list[str]] = lambda _parameters: []

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
            default_parameters=self._default_parameters(),
            reports=list(self.reports),
            supports_backtest=self.supports_backtest,
            backtest_comparisons=list(self.backtest_comparisons),
            messages={**PARAMETER_PROBLEM_TEXTS, **self.messages},
        )

    def parse_parameters(self, raw: dict[str, Any]) -> Any:
        try:
            parsed = self.parameters.model_validate(raw)
        except ValidationError as error:
            detail = jsonable_encoder(error.errors(include_url=False, include_context=False))
            raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, detail=detail) from error

        problems = [meaning_problem(problem) for problem in self.semantic_check(parsed)]
        if problems:
            detail = [problem.model_dump(by_alias=True) for problem in problems]
            raise HTTPException(status.HTTP_422_UNPROCESSABLE_CONTENT, detail=detail)

        return parsed

    def check(self, raw: dict[str, Any]) -> ParametersCheck:
        try:
            parsed = self.parameters.model_validate(raw)
        except ValidationError as error:
            return ParametersCheck(problems=shape_problems(raw, error))

        problems = [meaning_problem(problem) for problem in self.semantic_check(parsed)]

        return ParametersCheck(problems=problems, signal_ids=self.referenced_signals(parsed))

    def _default_parameters(self) -> dict[str, Any] | None:
        if not self.has_defaults:
            return None

        return self.parameters().model_dump(by_alias=True, mode="json")


SELECTION_COMPARISONS = (
    BacktestComparison.BENCHMARK,
    BacktestComparison.BENCHMARK_AT_EQUAL_RISK,
    BacktestComparison.UNIVERSE_EQUAL_WEIGHT,
)
HOLDING_COMPARISONS = (
    BacktestComparison.BENCHMARK,
    BacktestComparison.BENCHMARK_AT_EQUAL_RISK,
)

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
        backtest_comparisons=SELECTION_COMPARISONS,
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
        backtest_comparisons=SELECTION_COMPARISONS,
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
        backtest_comparisons=HOLDING_COMPARISONS,
        version=buy_and_hold.engine_version,
    ),
    rule_based.STRATEGY_CODE: StrategyType(
        code=rule_based.STRATEGY_CODE,
        name="Rule-based",
        description=msg("rule_based.description"),
        messages=rule_based.MESSAGES,
        parameters=RuleParameters,
        decide=rule_based.decide,
        assess_risk=rule_based_risk.assess_risk,
        required_series_roles=(SeriesRole.INSTRUMENT, SeriesRole.BENCHMARK),
        reports=rule_based.REPORTS,
        backtest_comparisons=SELECTION_COMPARISONS,
        version=rule_based.engine_version,
        has_defaults=False,
        semantic_check=problems_in,
        referenced_signals=referenced_signals,
    ),
}


def find_strategy_type(code: str) -> StrategyType:
    if code not in STRATEGY_TYPES:
        raise HTTPException(status.HTTP_404_NOT_FOUND, detail=f"Unknown strategy type '{code}'.")
    return STRATEGY_TYPES[code]


@router.get("/strategies", response_model=list[StrategyTypeDescriptor])
def list_strategy_types() -> list[StrategyTypeDescriptor]:
    return [strategy_type.descriptor() for strategy_type in STRATEGY_TYPES.values()]
