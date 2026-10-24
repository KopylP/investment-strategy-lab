"""Turn a scan result and the current portfolio into concrete monthly orders.

Rules (the same for the text report, Telegram and HTML):
1. Sell every risk position that is no longer selected.
2. Market OK: invest the whole contribution plus any parked cash.
   Market weak: invest ``weak_market_invest_pct`` of the contribution and park the rest.
   Sale proceeds are always reinvested into the current picks.
3. Each pick targets 1/top_n of the invested portfolio. Unfilled slots stay parked.
4. Top up underweight picks. Trim a pick only when it is in profit and more than
   ``trim_band`` over target; losing positions shrink as new money flows to the others.
5. Orders smaller than ``min_trade`` are skipped; whatever is not invested goes to the cash ETF.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import math

import pandas as pd

from .config import PlanSettings
from .scanner import ScanResult

KIND_EXIT = "exit"
KIND_TRIM = "trim"
KIND_UNPARK = "unpark"
KIND_BUY = "buy"
KIND_PARK = "park"

_ORDER_SEQUENCE = {KIND_EXIT: 0, KIND_TRIM: 1, KIND_UNPARK: 2, KIND_BUY: 3, KIND_PARK: 4}


@dataclass(frozen=True)
class Order:
    action: str
    kind: str
    ticker: str
    name: str
    amount: float
    shares: float
    price: float
    reason: str


@dataclass(frozen=True)
class AllocationRow:
    ticker: str
    name: str
    role: str
    value_before: float
    value_after: float
    weight_before: float
    weight_after: float
    target_weight: float | None


@dataclass(frozen=True)
class MonthlyPlan:
    market_ok: bool
    contribution: float
    invest_pct: float
    currency: str
    cash_etf: str | None
    top_n: int
    orders: list[Order]
    allocation: list[AllocationRow]
    total_before: float
    total_after: float
    parked_before: float
    parked_after: float
    notes: list[str] = field(default_factory=list)
    settings: PlanSettings | None = None

    @property
    def sells(self) -> list[Order]:
        return [order for order in self.orders if order.action == "sell"]

    @property
    def buys(self) -> list[Order]:
        return [order for order in self.orders if order.action == "buy"]


def build_plan(
    scan: ScanResult,
    *,
    holdings: dict[str, float],
    prices: dict[str, float],
    cash: float,
    settings: PlanSettings,
    top_n: int,
    cash_etf: str | None = None,
    names: dict[str, str] | None = None,
    currency: str = "EUR",
    contribution: float | None = None,
    cost_basis: dict[str, float] | None = None,
) -> MonthlyPlan:
    names = names or {}
    cost_basis = cost_basis or {}
    contribution = settings.monthly_contribution if contribution is None else contribution
    picks = list(scan.eligible.index)
    target_weight = 1.0 / top_n
    notes: list[str] = []

    def name(ticker: str) -> str:
        return names.get(ticker, ticker)

    def price(ticker: str) -> float | None:
        value = prices.get(ticker)
        return float(value) if value is not None and not pd.isna(value) and value > 0 else None

    values: dict[str, float] = {}
    for ticker, shares in holdings.items():
        latest = price(ticker)
        if latest is None:
            notes.append(f"Немає ціни для {ticker}: перевірте цю позицію вручну.")
            continue
        values[ticker] = shares * latest

    cash_etf_value = values.pop(cash_etf, 0.0) if cash_etf else 0.0
    parked_before = cash_etf_value + cash
    total_before = sum(values.values()) + parked_before

    orders: list[Order] = []

    def add_order(action: str, kind: str, ticker: str, amount: float, reason: str, shares: float | None = None) -> float:
        latest = price(ticker)
        if latest is None or amount <= 0:
            return 0.0
        if shares is None:
            shares = amount / latest
            if not settings.fractional_shares:
                shares = math.floor(shares)
                amount = shares * latest
        if shares <= 0:
            return 0.0
        orders.append(Order(action, kind, ticker, name(ticker), amount, shares, latest, reason))
        return amount

    # 1. Exit positions that dropped out of the selection.
    proceeds = 0.0
    for ticker, value in values.items():
        if ticker in picks:
            continue
        proceeds += add_order("sell", KIND_EXIT, ticker, value, _exit_reason(scan, ticker), shares=holdings[ticker])

    kept = {ticker: values[ticker] for ticker in picks if ticker in values}

    # 2. Money to invest this month.
    invest_pct = 1.0 if scan.market_ok else settings.weak_market_invest_pct
    deployable = contribution * invest_pct + proceeds + (parked_before if scan.market_ok else 0.0)
    investable_total = sum(kept.values()) + deployable
    target_value = investable_total * target_weight

    # 3. Trim winners that grew far above target.
    trims = 0.0
    for ticker, value in kept.items():
        excess = value - target_value
        in_profit = price(ticker) > cost_basis.get(ticker, 0.0)
        if in_profit and excess > settings.trim_band * investable_total and excess >= settings.min_trade:
            sold = add_order("sell", KIND_TRIM, ticker, excess, f"вага вище цілі {target_weight:.0%} більш ніж на {settings.trim_band:.0%}: продати надлишок")
            kept[ticker] = value - sold
            trims += sold
    deployable += trims

    # 4. Top up underweight picks, scaled down if money is short.
    gaps = {ticker: max(0.0, target_value - kept.get(ticker, 0.0)) for ticker in picks}
    total_gap = sum(gaps.values())
    scale = min(1.0, deployable / total_gap) if total_gap > 0 else 0.0
    skipped = []
    bought = 0.0
    for ticker in picks:
        amount = gaps[ticker] * scale
        if amount <= 0:
            continue
        if amount < settings.min_trade:
            skipped.append(ticker)
            continue
        bought += add_order("buy", KIND_BUY, ticker, amount, _buy_reason(scan, ticker, ticker in kept, target_weight))

    # 5. Whatever was not invested moves to (or comes from) the cash ETF.
    remaining = cash + contribution + proceeds + trims - bought
    if remaining < 0 and cash_etf:
        needed = -remaining / price(cash_etf)
        shares = needed if settings.fractional_shares else math.ceil(needed)
        shares = min(shares, holdings.get(cash_etf, 0.0))
        add_order("sell", KIND_UNPARK, cash_etf, shares * price(cash_etf), "запарковані кошти повертаються в роботу", shares=shares)
    elif remaining >= settings.min_trade and cash_etf and price(cash_etf) is not None:
        add_order("buy", KIND_PARK, cash_etf, remaining, "парковка: гроші, які цього місяця не інвестуються")

    orders.sort(key=lambda order: _ORDER_SEQUENCE[order.kind])

    total_after = total_before + contribution
    after = {ticker: kept.get(ticker, 0.0) for ticker in picks}
    for order in orders:
        if order.kind == KIND_BUY:
            after[order.ticker] = after.get(order.ticker, 0.0) + order.amount
    parked_after = total_after - sum(after.values())

    notes.extend(_notes(scan, picks, top_n, contribution, invest_pct, parked_before, cash_etf, currency, skipped, settings))

    allocation = _allocation(values, after, picks, cash_etf, parked_before, parked_after, total_before, total_after, target_weight, name)
    return MonthlyPlan(
        market_ok=scan.market_ok,
        contribution=contribution,
        invest_pct=invest_pct,
        currency=currency,
        cash_etf=cash_etf,
        top_n=top_n,
        orders=orders,
        allocation=allocation,
        total_before=total_before,
        total_after=total_after,
        parked_before=parked_before,
        parked_after=parked_after,
        notes=notes,
        settings=settings,
    )


def _exit_reason(scan: ScanResult, ticker: str) -> str:
    if ticker not in scan.table.index:
        return "не входить до списку ETF стратегії"
    reason = scan.table.loc[ticker, "Reason"]
    return f"випав із вибірки: {reason}" if reason else "випав із вибірки"


def _buy_reason(scan: ScanResult, ticker: str, held: bool, target_weight: float) -> str:
    row = scan.table.loc[ticker]
    momentum = row["Momentum"]
    detail = f"ранг {int(row['Rank'])}, моментум {momentum:+.1%}" if pd.notna(momentum) else f"ранг {int(row['Rank'])}"
    action = "докупити до цілі" if held else "нова позиція, ціль"
    return f"{action} {target_weight:.0%} портфеля ({detail})"


def _notes(
    scan: ScanResult,
    picks: list[str],
    top_n: int,
    contribution: float,
    invest_pct: float,
    parked_before: float,
    cash_etf: str | None,
    currency: str,
    skipped: list[str],
    settings: PlanSettings,
) -> list[str]:
    from finance_decisions.helpers import money

    parking = cash_etf or "кеш на рахунку"
    notes = []
    if contribution <= 0:
        notes.append("Внесок цього місяця дорівнює 0. Вкажіть monthly_contribution у config або передайте --contribution.")
    if not scan.market_ok:
        notes.append(
            f"Ринок слабкий: інвестуємо {invest_pct:.0%} внеску ({money(contribution * invest_pct, currency)}), "
            f"решту ({money(contribution * (1 - invest_pct), currency)}) паркуємо в {parking}. "
            "Через стан ринку поточні позиції не продаємо."
        )
    elif parked_before >= settings.min_trade:
        notes.append(f"Ринок у нормі: запарковані кошти ({money(parked_before, currency)}) повертаються в роботу.")
    if not picks:
        notes.append(f"Жоден ETF не пройшов фільтри: усі гроші лишаються в {parking}.")
    elif len(picks) < top_n:
        empty = top_n - len(picks)
        notes.append(
            f"Знайдено {len(picks)} з {top_n} кандидатів: {empty} порожн{'ій слот' if empty == 1 else 'і слоти'} "
            f"({empty / top_n:.0%} портфеля) лишаються в {parking} до наступного місяця."
        )
    if skipped:
        notes.append(f"Пропущено дрібні покупки (менше {money(settings.min_trade, currency)}): {', '.join(skipped)}.")
    return notes


def _allocation(
    values: dict[str, float],
    after: dict[str, float],
    picks: list[str],
    cash_etf: str | None,
    parked_before: float,
    parked_after: float,
    total_before: float,
    total_after: float,
    target_weight: float,
    name,
) -> list[AllocationRow]:
    def weight(value: float, total: float) -> float:
        return value / total if total > 0 else 0.0

    rows = [
        AllocationRow(t, name(t), "pick", values.get(t, 0.0), after.get(t, 0.0), weight(values.get(t, 0.0), total_before), weight(after.get(t, 0.0), total_after), target_weight)
        for t in picks
    ]
    rows += [
        AllocationRow(t, name(t), "exit", value, 0.0, weight(value, total_before), 0.0, 0.0)
        for t, value in values.items()
        if t not in picks
    ]
    rows.append(
        AllocationRow(
            cash_etf or "CASH",
            name(cash_etf) if cash_etf else "Кеш",
            "cash",
            parked_before,
            parked_after,
            weight(parked_before, total_before),
            weight(parked_after, total_after),
            None,
        )
    )
    return rows
