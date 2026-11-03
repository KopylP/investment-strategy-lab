from __future__ import annotations

import math

import numpy as np
import pandas as pd
import pytest

from finance_decisions.api.contracts import EvaluationContext
from finance_decisions.strategies.errors import ContextError
from finance_decisions.strategies.rule_based.indicators import history_needed, indicator_series
from finance_decisions.strategies.rule_based.parameters import RuleParameters
from finance_decisions.strategies.rule_based.series_book import SeriesBook

FUND = {"series": "fund"}


def _indicator(**definition):
    rules = {
        "formatVersion": 1,
        "indicators": {"x": definition},
        "selection": {"top": 1},
        "weights": {"method": "equal"},
    }
    return RuleParameters.model_validate(rules).indicators["x"]


def _prices(values) -> pd.Series:
    return pd.Series(values, index=pd.bdate_range("2024-01-01", periods=len(values)), dtype=float)


def _last(definition, prices: pd.Series, benchmark: pd.Series | None = None) -> float:
    return float(indicator_series(definition, prices, benchmark).iloc[-1])


def test_sma_is_the_mean_of_the_last_days() -> None:
    series = indicator_series(_indicator(kind="sma", of=FUND, days=4), _prices(range(1, 11)), None)

    assert series.iloc[-1] == pytest.approx(8.5)
    assert series.iloc[:3].isna().all()
    assert not math.isnan(series.iloc[3])


def test_price_vs_sma_is_the_relative_distance() -> None:
    prices = _prices([100.0] * 10 + [110.0])

    assert _last(_indicator(kind="priceVsSma", of=FUND, days=5), prices) == pytest.approx(110 / 102 - 1)


def test_momentum_looks_back_21_trading_days_per_month() -> None:
    prices = _prices(100 * 1.01 ** np.arange(60))

    assert _last(_indicator(kind="momentum", of=FUND, months=1), prices) == pytest.approx(prices.iloc[-1] / prices.iloc[-22] - 1)


def test_blended_momentum_is_nan_until_twelve_months_exist() -> None:
    definition = _indicator(kind="blendedMomentum", of=FUND)
    short = _prices(100 * 1.001 ** np.arange(200))
    long = _prices(100 * 1.001 ** np.arange(260))
    expected = np.mean([long.iloc[-1] / long.iloc[-1 - months * 21] - 1 for months in (3, 6, 12)])

    assert math.isnan(_last(definition, short))
    assert _last(definition, long) == pytest.approx(expected)


def test_relative_strength_subtracts_the_benchmark_return() -> None:
    fund = _prices([100.0] * 10 + [110.0] * 12)
    benchmark = _prices([100.0] * 10 + [104.0] * 12)

    assert _last(_indicator(kind="relativeStrength", of=FUND, months=1), fund, benchmark) == pytest.approx(0.06)


def test_volatility_is_annualised() -> None:
    returns = np.array([0.01, -0.01] * 15)
    prices = _prices(100 * np.cumprod(np.concatenate([[1.0], 1 + returns])))
    expected = pd.Series(returns[-10:]).std() * math.sqrt(252)

    assert _last(_indicator(kind="volatility", of=FUND, days=10), prices) == pytest.approx(expected, abs=1e-9)


def test_drawdown_is_measured_from_the_rolling_peak() -> None:
    assert _last(_indicator(kind="drawdown", of=FUND, days=5), _prices([100.0, 120.0, 90.0, 95.0, 90.0])) == pytest.approx(-0.25)


def test_rsi_of_a_rising_series_is_100() -> None:
    assert _last(_indicator(kind="rsi", of=FUND, days=14), _prices(np.arange(1, 40))) == pytest.approx(100.0)


def test_sharpe_is_the_annual_return_over_the_annual_volatility() -> None:
    returns = np.array([0.012, -0.004] * 30)
    prices = _prices(100 * np.cumprod(np.concatenate([[1.0], 1 + returns])))
    window = pd.Series(returns[-21:])
    expected = window.mean() * 252 / (window.std() * math.sqrt(252))

    assert _last(_indicator(kind="sharpe", of=FUND, months=1), prices) == pytest.approx(expected)


def test_history_needed_counts_trading_days() -> None:
    assert history_needed(_indicator(kind="blendedMomentum", of=FUND)) == 253
    assert history_needed(_indicator(kind="sma", of=FUND, days=200)) == 200
    assert history_needed(_indicator(kind="momentum", of=FUND, months=6)) == 127


def _context(series: list[dict]) -> EvaluationContext:
    return EvaluationContext.model_validate(
        {
            "asOf": "2024-01-03",
            "universe": [{"key": "AAA"}],
            "marketSeries": series,
            "subPortfolio": {"currency": "EUR"},
        }
    )


def _series(key: str, role: str) -> dict:
    return {"key": key, "role": role, "dates": ["2024-01-01", "2024-01-02", "2024-01-03", "2024-01-04"], "adjustedClose": [1.0, 2.0, 3.0, 4.0]}


def test_series_book_cuts_every_series_at_as_of() -> None:
    book = SeriesBook.from_context(_context([_series("AAA", "instrument"), _series("SPY", "benchmark")]))

    assert list(book.fund("AAA")) == [1.0, 2.0, 3.0]
    assert list(book.benchmark()) == [1.0, 2.0, 3.0]
    assert list(book.funds_frame().columns) == ["AAA"]


def test_an_extra_series_is_found_by_its_instrument_also_among_funds() -> None:
    book = SeriesBook.from_context(_context([_series("AAA", "instrument"), _series("GOLD", "extra")]))

    assert list(book.of(_indicator(kind="price", of={"series": "extra", "instrumentId": "GOLD"}).of, None)) == [1.0, 2.0, 3.0]
    assert list(book.of(_indicator(kind="price", of={"series": "extra", "instrumentId": "AAA"}).of, None)) == [1.0, 2.0, 3.0]


def test_a_missing_series_refuses_the_context_naming_its_role() -> None:
    book = SeriesBook.from_context(_context([_series("AAA", "instrument")]))

    with pytest.raises(ContextError, match="benchmark"):
        book.benchmark()
    with pytest.raises(ContextError, match="extra"):
        book.of(_indicator(kind="price", of={"series": "extra", "instrumentId": "GOLD"}).of, None)
