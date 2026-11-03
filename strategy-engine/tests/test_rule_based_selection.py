from __future__ import annotations

import math

import numpy as np
import pandas as pd
import pytest

from finance_decisions.api.contracts import EvaluationContext
from finance_decisions.strategies.rule_based.conditions import ConditionReader
from finance_decisions.strategies.rule_based.parameters import RuleParameters, Weights
from finance_decisions.strategies.rule_based.selection import FundVerdict, judge_funds
from finance_decisions.strategies.rule_based.series_book import SeriesBook
from finance_decisions.strategies.rule_based.weights import WeightPlan, weights_for
from test_api_contracts import _context, _frame

INDICATORS = {
    "mom": {"kind": "momentum", "of": {"series": "fund"}, "months": 6},
    "price": {"kind": "price", "of": {"series": "fund"}},
}
NEVER = {"kind": "compare", "left": {"indicator": "price"}, "operator": "gt", "right": {"value": 1e12}}


def _judge(rules: dict, frame: pd.DataFrame | None = None, held: set[str] | None = None) -> list[FundVerdict]:
    parameters = RuleParameters.model_validate({"formatVersion": 1, "indicators": INDICATORS, "weights": {"method": "equal"}, **rules})
    context = EvaluationContext.model_validate(_context(frame if frame is not None else _frame()))
    book = SeriesBook.from_context(context)
    reader = ConditionReader(parameters, book, context.signals)

    return judge_funds(parameters, reader, context.universe, held or set(), book.funds_frame())


def _statuses(verdicts: list[FundVerdict]) -> dict[str, str]:
    return {verdict.key: verdict.status for verdict in verdicts}


def _selected(verdicts: list[FundVerdict]) -> list[str]:
    return [verdict.key for verdict in verdicts if verdict.status == "selected"]


def test_a_held_fund_stays_until_its_exit_rule_passes() -> None:
    verdicts = _judge({"entry": {"condition": NEVER}, "exit": {"condition": NEVER}, "selection": {"top": 3}}, held={"AAA"})

    assert _selected(verdicts) == ["AAA"]
    assert _statuses(verdicts)["BBB"] == "failed"


def test_a_held_fund_leaves_when_its_exit_rule_passes() -> None:
    always = {"kind": "not", "condition": NEVER}
    verdicts = _judge({"exit": {"condition": always}, "selection": {"top": 3}}, held={"AAA"})

    assert _statuses(verdicts)["AAA"] == "exited"


def test_without_an_exit_rule_a_held_fund_stays_a_candidate() -> None:
    verdicts = _judge({"entry": {"condition": NEVER}, "selection": {"top": 3}}, held={"AAA"})

    assert _selected(verdicts) == ["AAA"]


def test_candidates_are_ranked_by_the_chosen_indicator() -> None:
    strongest_first = _judge({"selection": {"rankBy": "mom", "top": 1}})
    weakest_first = _judge({"selection": {"rankBy": "mom", "order": "asc", "top": 1}})

    descending = [verdict.score for verdict in strongest_first if verdict.rank is not None]
    assert descending == sorted(descending, reverse=True)
    assert _selected(weakest_first) == ["FFF"]
    assert _selected(strongest_first) != ["FFF"]


def test_without_a_ranking_the_universe_order_is_kept() -> None:
    assert _selected(_judge({"selection": {"top": 2}})) == ["AAA", "BBB"]


def test_one_fund_per_group() -> None:
    verdicts = _judge({"selection": {"rankBy": "mom", "top": 6, "maxPerGroup": 1}})
    group_a = {_statuses(verdicts)["AAA"], _statuses(verdicts)["EEE"]}

    assert group_a == {"selected", "group_blocked"}


def test_a_fund_too_similar_to_a_chosen_one_is_skipped() -> None:
    frame = _frame()
    frame["DDD"] = frame["CCC"] * 1.001

    statuses = _statuses(_judge({"selection": {"rankBy": "mom", "top": 6, "maxCorrelation": 0.7}}, frame))

    assert {statuses["CCC"], statuses["DDD"]} == {"selected", "correlated"}


def test_a_fund_without_prices_is_never_picked() -> None:
    frame = _frame()
    frame["AAA"] = np.nan

    assert _statuses(_judge({"selection": {"top": 6}}, frame))["AAA"] == "failed"


def _noisy(volatility: float, seed: int, days: int = 120) -> np.ndarray:
    returns = np.random.default_rng(seed).normal(0, volatility / math.sqrt(252), days)
    return 100 * np.exp(np.cumsum(returns))


def _prices(**columns: np.ndarray) -> pd.DataFrame:
    length = len(next(iter(columns.values())))
    return pd.DataFrame(columns, index=pd.bdate_range("2024-01-01", periods=length))


def test_equal_weights_leave_free_slots_in_cash() -> None:
    plan = weights_for(["A", "B"], _prices(A=_noisy(0.1, 1), B=_noisy(0.1, 2)), Weights(method="equal"), top=3)

    assert plan.weights == pytest.approx({"A": 1 / 3, "B": 1 / 3})
    assert plan.share == 1.0


def test_inverse_volatility_gives_the_calmer_fund_more() -> None:
    returns = np.array([0.01, -0.01] * 40)
    calm = 100 * np.cumprod(np.concatenate([[1.0], 1 + returns]))
    wild = 100 * np.cumprod(np.concatenate([[1.0], 1 + 2 * returns]))

    plan = weights_for(["CALM", "WILD"], _prices(CALM=calm, WILD=wild), Weights(method="inverseVolatility"), top=3)

    assert plan.weights["CALM"] == pytest.approx(2 * plan.weights["WILD"])
    assert sum(plan.weights.values()) == pytest.approx(2 / 3)


def test_max_per_fund_caps_and_keeps_the_rest_in_cash() -> None:
    plan = weights_for(["A"], _prices(A=_noisy(0.1, 1)), Weights(method="equal", maxPerFund=0.4), top=1)

    assert plan.weights == pytest.approx({"A": 0.4})


def test_target_volatility_scales_everything_down() -> None:
    prices = _prices(A=_noisy(0.3, 1), B=_noisy(0.3, 2))

    plan = weights_for(["A", "B"], prices, Weights(method="equal", targetVolatility=0.02), top=2)

    assert plan.volatility is not None and plan.volatility > 0.02
    assert plan.share == pytest.approx(0.02 / plan.volatility)
    assert plan.weights == pytest.approx({"A": 0.5 * plan.share, "B": 0.5 * plan.share})


def test_no_picks_give_no_targets_and_no_error() -> None:
    verdicts = _judge({"entry": {"condition": NEVER}, "selection": {"top": 3}})

    plan = weights_for(_selected(verdicts), _frame(), Weights(method="inverseVolatility", targetVolatility=0.1), top=3)

    assert plan == WeightPlan({}, 1.0, None)
