from __future__ import annotations

import pandas as pd

from finance_decisions.api.contracts import EvaluationContext, Holding, PortfolioRisk, RiskAssessment, RiskSignal, RiskSignalKind
from finance_decisions.messages import msg
from finance_decisions.portfolio_math import TRADING_DAYS_PER_YEAR
from finance_decisions.strategies.errors import ContextError
from .conditions import ConditionReader
from .decision import STRATEGY_CODE, engine_version
from .parameters import RuleParameters
from .series_book import SeriesBook

STATUS_OK = "ok"
STATUS_ACT = "act"


def assess_risk(context: EvaluationContext, parameters: RuleParameters) -> RiskAssessment:
    book = SeriesBook.from_context(context)
    reader = ConditionReader(parameters, book, context.signals)

    candidates = [_exit_signal(holding, parameters, reader, book) for holding in context.sub_portfolio.holdings]
    signals = [signal for signal in candidates if signal is not None]
    actions = [signal.reason for signal in signals]

    return RiskAssessment(
        strategy_code=STRATEGY_CODE,
        engine_version=engine_version(),
        as_of=context.as_of,
        status=STATUS_ACT if signals else STATUS_OK,
        market_alerts=[],
        positions=[],
        portfolio=_portfolio(context, book),
        signals=signals,
        actions=actions,
    )


def _exit_signal(holding: Holding, parameters: RuleParameters, reader: ConditionReader, book: SeriesBook) -> RiskSignal | None:
    prices = book.valuation_fund(holding.key).dropna()
    if not book.is_fresh(prices):
        raise ContextError(f"no fresh valuation price for held instrument {holding.key}")

    price = float(prices.iloc[-1])
    reasons = _stop_reasons(holding, price, prices, parameters) + _exit_rule_reasons(holding, parameters, reader)
    if not reasons:
        return None

    reason = msg("rule_based.risk.reasons", items=reasons)

    return RiskSignal(key=holding.key, kind=RiskSignalKind.EXIT, reason=reason, exit_price=price)


def _stop_reasons(holding: Holding, price: float, prices: pd.Series, parameters: RuleParameters) -> list[str]:
    risk = parameters.risk
    if risk is None:
        return []

    reasons = []
    loss = price / _average_cost(holding) - 1
    if risk.stop_loss_from_cost is not None and loss <= risk.stop_loss_from_cost:
        reasons.append(msg("rule_based.risk.stop_loss", loss=loss, limit=risk.stop_loss_from_cost))

    drawdown = price / _peak_since_bought(holding, prices) - 1
    if risk.drawdown_from_peak is not None and drawdown <= risk.drawdown_from_peak:
        reasons.append(msg("rule_based.risk.drawdown", drawdown=drawdown, limit=risk.drawdown_from_peak))

    return reasons


def _exit_rule_reasons(holding: Holding, parameters: RuleParameters, reader: ConditionReader) -> list[str]:
    if parameters.exit is None:
        return []

    outcome = reader.read(parameters.exit.condition, holding.key)

    return [msg("rule_based.risk.exit_rule", items=outcome.reasons)] if outcome.passed else []


def _average_cost(holding: Holding) -> float:
    return sum(lot.quantity * lot.price for lot in holding.lots) / holding.quantity


def _peak_since_bought(holding: Holding, prices: pd.Series) -> float:
    bought = [lot.acquired_on for lot in holding.lots if lot.acquired_on is not None]
    if len(bought) < len(holding.lots):
        return float(prices.tail(TRADING_DAYS_PER_YEAR).max())

    since = prices.loc[pd.Timestamp(min(bought)) :]

    return float(since.max()) if len(since) else float(prices.iloc[-1])


def _portfolio(context: EvaluationContext, book: SeriesBook) -> PortfolioRisk:
    cash = context.sub_portfolio.cash
    value, cost = cash, cash
    for holding in context.sub_portfolio.holdings:
        prices = book.valuation_fund(holding.key).dropna()
        price = float(prices.iloc[-1]) if len(prices) else _average_cost(holding)
        value += holding.quantity * price
        cost += holding.quantity * _average_cost(holding)

    return PortfolioRisk(value=value, cost=cost, pnl_pct=None, drawdown=None, peak_date=None, exit_limit_hit=False, alerts=[])
