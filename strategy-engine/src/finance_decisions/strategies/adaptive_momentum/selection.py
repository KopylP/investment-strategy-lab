from __future__ import annotations

from dataclasses import dataclass

import pandas as pd

from finance_decisions.messages import msg
from finance_decisions.portfolio_math import pairwise_correlation
from .fund_reading import FundReading

STATUS_SELECTED = "selected"
STATUS_RESERVE = "reserve"
STATUS_GROUP_BLOCKED = "group_blocked"
STATUS_CORRELATED = "correlated"
STATUS_FAILED = "failed"

CORRELATION_WINDOW_DAYS = 126
MIN_CORRELATION_DAYS = 60


@dataclass(frozen=True)
class SelectionRules:
    top_n: int
    trend_days: int
    max_correlation: float
    max_per_group: int


@dataclass(frozen=True)
class Verdict:
    reading: FundReading
    status: str
    reason: str
    rank: int | None = None

    @property
    def selected(self) -> bool:
        return self.status == STATUS_SELECTED


def select(readings: list[FundReading], prices: pd.DataFrame, rules: SelectionRules) -> list[Verdict]:
    qualified = _strongest_first([reading for reading in readings if not reading.failures(rules.trend_days)])
    failed = [_failed(reading, rules.trend_days) for reading in readings if reading.failures(rules.trend_days)]

    ranked = _rank(qualified, prices, rules)

    return ranked + failed


def _strongest_first(readings: list[FundReading]) -> list[FundReading]:
    return sorted(readings, key=lambda reading: reading.momentum, reverse=True)


def _failed(reading: FundReading, trend_days: int) -> Verdict:
    reason = msg("adaptive_momentum.fund.failures", items=reading.failures(trend_days))

    return Verdict(reading, STATUS_FAILED, reason)


def _rank(qualified: list[FundReading], prices: pd.DataFrame, rules: SelectionRules) -> list[Verdict]:
    verdicts: list[Verdict] = []
    chosen: list[FundReading] = []

    for rank, reading in enumerate(qualified, start=1):
        verdict = _judge(reading, rank, chosen, prices, rules)
        if verdict.selected:
            chosen.append(reading)
        verdicts.append(verdict)

    return verdicts


def _judge(reading: FundReading, rank: int, chosen: list[FundReading], prices: pd.DataFrame, rules: SelectionRules) -> Verdict:
    group_members = [other.name for other in chosen if other.group == reading.group]
    if len(group_members) >= rules.max_per_group:
        reason = msg("adaptive_momentum.fund.group_taken", group=reading.group, members=group_members)
        return Verdict(reading, STATUS_GROUP_BLOCKED, reason, rank)

    twin = _too_similar_to(reading, chosen, prices, rules.max_correlation)
    if twin is not None:
        reason = msg("adaptive_momentum.fund.correlated", peer=twin.name, limit=rules.max_correlation)
        return Verdict(reading, STATUS_CORRELATED, reason, rank)

    if len(chosen) >= rules.top_n:
        return Verdict(reading, STATUS_RESERVE, msg("adaptive_momentum.fund.stronger_exist", rank=rank), rank)

    return Verdict(reading, STATUS_SELECTED, msg("adaptive_momentum.fund.ranked", rank=rank, momentum=reading.momentum), rank)


def _too_similar_to(reading: FundReading, chosen: list[FundReading], prices: pd.DataFrame, limit: float) -> FundReading | None:
    if limit >= 1.0:
        return None

    for other in chosen:
        if pairwise_correlation(prices, reading.key, other.key, CORRELATION_WINDOW_DAYS, MIN_CORRELATION_DAYS) > limit:
            return other

    return None

