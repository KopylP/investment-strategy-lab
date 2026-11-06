from datetime import timedelta

import numpy as np
import pandas as pd
import pytest
from fastapi.testclient import TestClient

from finance_decisions.api.main import create_app
from finance_decisions.api.contracts import EvaluationContext
from finance_decisions.strategies.errors import ContextError
from finance_decisions.strategies.rule_based.conditions import ConditionReader
from finance_decisions.strategies.rule_based.parameters import RuleParameters
from finance_decisions.strategies.rule_based.risk import assess_risk
from finance_decisions.strategies.rule_based.series_book import SeriesBook
from finance_decisions.strategies.rule_based.decision import decide
from finance_decisions.strategies.rule_based.selection import judge_funds
from test_rule_based_conditions import _reader, _context, _condition, _compare, _rising
from test_rule_based_selection import _statuses


def _judge(rules, frame):
    parameters = RuleParameters.model_validate({'formatVersion': 1, 'indicators': {}, 'weights': {'method': 'equal'}, **rules})
    context = _context({key: list(frame[key]) for key in frame}, list(frame.iloc[:, 0]))
    book = SeriesBook.from_context(context)
    return judge_funds(parameters, ConditionReader(parameters, book, None), context.universe, set(), book.funds_frame())


def test_not_cannot_turn_short_history_into_an_entry_signal():
    reader = _reader(_context({'AAA': _rising(100)}, _rising()))
    unknown = _compare('long', 'gt', {'value': 0})
    for condition in (unknown, {'kind': 'not', 'condition': unknown},
                      {'kind': 'not', 'condition': {'kind': 'not', 'condition': unknown}}):
        result = reader.read(_condition(**condition), 'AAA')
        assert not result.passed
        assert not result.known


@pytest.mark.parametrize('kind,known_side,known,passed', [
    ('all', False, True, False), ('all', True, False, False),
    ('any', False, False, False), ('any', True, True, True),
])
def test_logic_preserves_unknown_unless_a_known_branch_decides_the_result(kind, known_side, known, passed):
    reader = _reader(_context({'AAA': _rising(100)}, _rising()))
    short = _compare('long', 'gt', {'value': 0})
    other = _compare('price', 'gt' if known_side else 'lt', {'value': 0})
    result = reader.read(_condition(kind=kind, conditions=[short, other]), 'AAA')
    assert result.known is known
    assert result.passed is passed
    negated = reader.read(_condition(kind='not', condition={'kind': kind, 'conditions': [short, other]}), 'AAA')
    assert negated.known is known
    assert negated.passed is (known and not passed)


@pytest.mark.parametrize('other', [None, 'spy'])
def test_crossing_inside_the_window_is_detected_even_if_endpoints_are_on_the_same_side(other):
    reader = _reader(_context({'AAA': [110., 90., 110., 110.]}, [100.] * 4))
    target = {'other': other} if other else {'level': 100}
    assert reader.read(_condition(kind='crossed', indicator='price', direction='up', withinDays=3, **target), 'AAA').passed


def test_crossing_outside_the_window_is_not_detected():
    reader = _reader(_context({'AAA': [90., 110., 110., 110., 110.]}, [100.] * 5))
    assert not reader.read(_condition(kind='crossed', indicator='price', direction='up', withinDays=2, level=100), 'AAA').passed


def test_missing_session_inside_crossing_window_is_unknown_not_a_fabricated_crossing():
    reader = _reader(_context({'AAA': [90., None, 110., 110.]}, [100.] * 4))
    result = reader.read(_condition(kind='crossed', indicator='price', direction='up', withinDays=3, level=100), 'AAA')
    assert not result.passed
    assert not result.known


@pytest.mark.parametrize('window', [20, 59, 60])
def test_short_allowed_correlation_windows_reject_identical_assets(window):
    dates = pd.bdate_range('2024-01-01', periods=100)
    prices = 100 * np.cumprod(1 + np.tile([.01, -.005, .02, -.01], 25))
    frame = pd.DataFrame({'AAA': prices, 'BBB': prices * 2}, index=dates)
    statuses = _statuses(_judge({'selection': {'top': 2, 'maxCorrelation': .7, 'correlationDays': window}}, frame))
    assert statuses['AAA'] == 'selected'
    assert statuses['BBB'] == 'correlated'


def test_unknown_correlation_is_reported_and_does_not_bypass_the_limit():
    frame = pd.DataFrame({'AAA': [100.] * 10, 'BBB': [200.] * 10}, index=pd.bdate_range('2024-01-01', periods=10))
    verdicts = _judge({'selection': {'top': 2, 'maxCorrelation': .7, 'correlationDays': 20}}, frame)
    assert _statuses(verdicts)['BBB'] == 'failed'
    assert next(v.reason for v in verdicts if v.key == 'BBB').startswith('@rule_based.fund.correlation_unknown')


@pytest.mark.parametrize('age,allowed', [(3, True), (7, True), (90, False)])
def test_quote_freshness_allows_holidays_but_not_indefinitely_stale_prices(age, allowed):
    context = _context({'AAA': [100., 101.]}, [100., 101.])
    context = context.model_copy(update={'as_of': context.as_of + timedelta(days=age)})
    reader = _reader(context)
    result = reader.read(_condition(**_compare('price', 'gt', {'value': 0})), 'AAA')
    assert result.passed is allowed
    assert result.known is allowed
    rules = RuleParameters.model_validate({'formatVersion': 1, 'indicators': {}, 'selection': {'top': 1}, 'weights': {'method': 'equal'}})
    assert bool(decide(context, rules).targets) is allowed


def test_range_check_places_a_reversed_interval_at_its_nested_node():
    with TestClient(create_app()) as client:
        rules = {'formatVersion': 1, 'indicators': {}, 'entry': {'condition': {'kind': 'not', 'condition':
            {'kind': 'between', 'left': {'value': 100}, 'low': 110, 'high': 90}}},
            'selection': {'top': 1}, 'weights': {'method': 'equal'}}
        check = client.post('/v1/strategies/rule-based/parameters/check', json={'parameters': rules}).json()
        assert check['problems'][0]['path'] == ['entry', 'condition', 'condition', 'low']
        assert check['problems'][0]['code'] == 'rule_based.check.range_reversed'
        context = _context({'AAA': [100., 101.]}, [100., 101.]).model_dump(by_alias=True, mode='json')
        context['parameters'] = rules
        assert client.post('/v1/strategies/rule-based/decide', json=context).status_code == 422


def test_history_contract_includes_longest_indicator_and_crossing_lookback():
    rules = {'formatVersion': 1, 'indicators': {'long': {'kind': 'sharpe', 'of': {'series': 'fund'}, 'months': 36}},
             'entry': {'condition': {'kind': 'crossed', 'indicator': 'long', 'direction': 'up', 'level': 0, 'withinDays': 252}},
             'selection': {'top': 1}, 'weights': {'method': 'equal'}}
    with TestClient(create_app()) as client:
        response = client.post('/v1/strategies/rule-based/parameters/check', json={'parameters': rules})
    assert response.json()['historyBars'] == 36 * 21 + 1 + 252
    parameters = RuleParameters.model_validate(rules)
    values = list(100 * np.exp(np.cumsum(np.random.default_rng(42).normal(.001, .01, 1009))))
    context = _context({'AAA': values}, values)
    reader = ConditionReader(parameters, SeriesBook.from_context(context), None)
    assert reader.value_of('long', 'AAA') is not None
    assert reader.read(parameters.entry.condition, 'AAA').known


def test_replay_valuations_drive_stops_while_signal_prices_keep_their_currency():
    context = EvaluationContext.model_validate({'asOf': '2026-11-06', 'universe': [{'key': 'AAA'}],
        'marketSeries': [{'key': 'AAA', 'dates': ['2026-11-06'], 'adjustedClose': [90], 'valuationClose': [72]}],
        'subPortfolio': {'currency': 'EUR', 'holdings': [{'key': 'AAA', 'lots': [{'quantity': 1, 'price': 80, 'acquiredOn': '2026-01-01'}]}]}})
    parameters = RuleParameters.model_validate({'formatVersion': 1, 'indicators': {'price': {'kind': 'price', 'of': {'series': 'fund'}}},
        'selection': {'top': 1}, 'weights': {'method': 'equal'}, 'risk': {'stopLossFromCost': -.05}})
    risk = assess_risk(context, parameters)
    assert risk.signals[0].exit_price == 72
    assert risk.portfolio.value == 72
    reader = ConditionReader(parameters, SeriesBook.from_context(context), None)
    assert reader.value_of('price', 'AAA') == 90
    stale = context.model_copy(update={'as_of': context.as_of + timedelta(days=90)})
    with pytest.raises(ContextError, match='fresh valuation'):
        assess_risk(stale, parameters)
