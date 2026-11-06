from __future__ import annotations

from dataclasses import dataclass
import math

import pandas as pd

from finance_decisions.api.contracts import UniverseMember
from finance_decisions.messages import msg
from finance_decisions.portfolio_math import pairwise_correlation
from .conditions import ConditionReader
from .parameters import RuleParameters, Selection

STATUS_SELECTED = "selected"
STATUS_RESERVE = "reserve"
STATUS_GROUP_BLOCKED = "group_blocked"
STATUS_CORRELATED = "correlated"
STATUS_FAILED = "failed"
STATUS_EXITED = "exited"


@dataclass(frozen=True)
class FundVerdict:
    key: str
    name: str
    group: str
    status: str
    reason: str
    rank: int | None = None
    score: float | None = None

    @property
    def selected(self) -> bool:
        return self.status == STATUS_SELECTED


@dataclass(frozen=True)
class Candidate:
    member: UniverseMember
    score: float | None

    @property
    def key(self) -> str:
        return self.member.key

    @property
    def name(self) -> str:
        return self.member.name or self.member.key

    @property
    def group(self) -> str:
        return self.member.group or self.member.key


def judge_funds(
    parameters: RuleParameters,
    reader: ConditionReader,
    members: list[UniverseMember],
    held: set[str],
    prices: pd.DataFrame,
) -> list[FundVerdict]:
    candidates: list[Candidate] = []
    dropped: list[FundVerdict] = []

    for member in members:
        verdict = _gate(parameters, reader, member, member.key in held, prices)
        if verdict is not None:
            dropped.append(verdict)
            continue
        candidate, unscored = _scored(parameters.selection, reader, member)
        if unscored is not None:
            dropped.append(unscored)
            continue
        candidates.append(candidate)

    ranked = _rank(_ordered(candidates, parameters.selection), prices, parameters.selection)

    return ranked + dropped


def _gate(parameters: RuleParameters, reader: ConditionReader, member: UniverseMember, is_held: bool, prices: pd.DataFrame) -> FundVerdict | None:
    candidate = Candidate(member, None)
    if not _has_price(prices, member.key) or not reader.has_fresh_price(member.key):
        return _verdict(candidate, STATUS_FAILED, msg("rule_based.fund.no_data"))

    if is_held and parameters.exit is not None:
        outcome = reader.read(parameters.exit.condition, member.key)
        if outcome.passed:
            return _verdict(candidate, STATUS_EXITED, msg("rule_based.fund.exited", items=outcome.reasons))
    if not is_held and parameters.entry is not None:
        outcome = reader.read(parameters.entry.condition, member.key)
        if not outcome.passed:
            return _verdict(candidate, STATUS_FAILED, msg("rule_based.fund.failed", items=outcome.reasons))

    return None


def _has_price(prices: pd.DataFrame, key: str) -> bool:
    return key in prices.columns and prices[key].notna().any()


def _scored(selection: Selection, reader: ConditionReader, member: UniverseMember) -> tuple[Candidate, FundVerdict | None]:
    if selection.rank_by is None:
        return Candidate(member, None), None

    score = reader.value_of(selection.rank_by, member.key)
    candidate = Candidate(member, score)
    if score is None:
        reasons = reader.short_history(selection.rank_by).reasons
        return candidate, _verdict(candidate, STATUS_FAILED, msg("rule_based.fund.failed", items=reasons))

    return candidate, None


def _ordered(candidates: list[Candidate], selection: Selection) -> list[Candidate]:
    if selection.rank_by is None:
        return candidates

    return sorted(candidates, key=lambda candidate: candidate.score, reverse=selection.order == "desc")


def _rank(candidates: list[Candidate], prices: pd.DataFrame, selection: Selection) -> list[FundVerdict]:
    verdicts: list[FundVerdict] = []
    chosen: list[Candidate] = []

    for rank, candidate in enumerate(candidates, start=1):
        verdict = _judge(candidate, rank, chosen, prices, selection)
        if verdict.selected:
            chosen.append(candidate)
        verdicts.append(verdict)

    return verdicts


def _judge(candidate: Candidate, rank: int, chosen: list[Candidate], prices: pd.DataFrame, selection: Selection) -> FundVerdict:
    group_members = [other.name for other in chosen if other.group == candidate.group]
    if selection.max_per_group is not None and len(group_members) >= selection.max_per_group:
        reason = msg("rule_based.fund.group_taken", group=candidate.group, members=group_members)
        return _verdict(candidate, STATUS_GROUP_BLOCKED, reason, rank)

    if selection.max_correlation is not None and selection.max_correlation < 1.0:
        for other in chosen:
            correlation = pairwise_correlation(prices, candidate.key, other.key, selection.correlation_days)
            if not math.isfinite(correlation):
                return _verdict(candidate, STATUS_FAILED, msg("rule_based.fund.correlation_unknown", peer=other.name), rank)
            if correlation > selection.max_correlation:
                reason = msg("rule_based.fund.correlated", peer=other.name, limit=selection.max_correlation)
                return _verdict(candidate, STATUS_CORRELATED, reason, rank)

    if len(chosen) >= selection.top:
        return _verdict(candidate, STATUS_RESERVE, msg("rule_based.fund.stronger_exist", rank=rank), rank)

    return _verdict(candidate, STATUS_SELECTED, msg("rule_based.fund.ranked", rank=rank), rank)


def _verdict(candidate: Candidate, status: str, reason: str, rank: int | None = None) -> FundVerdict:
    return FundVerdict(
        key=candidate.key,
        name=candidate.name,
        group=candidate.group,
        status=status,
        reason=reason,
        rank=rank,
        score=candidate.score,
    )
