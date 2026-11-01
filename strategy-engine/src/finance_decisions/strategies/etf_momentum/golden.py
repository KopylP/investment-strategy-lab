"""Golden fixtures: inputs and outputs of ``build_plan`` for the .NET rebalancer port.

Each fixture holds exactly what the rebalancer gets in .NET (the strategy decision, the
sub-portfolio, prices, plan settings) and the orders plan.py produced from it. The .NET
golden tests must reproduce the orders to the cent. ``replay`` proves a fixture is complete.
"""

from __future__ import annotations

import math
from datetime import date
from typing import Any

import numpy as np
import pandas as pd

from finance_decisions.messages import msg, render, render_deep, t
from .backtest import PlanCapture, run_backtest
from .config import PlanSettings, StrategyConfig
from .plan import MonthlyPlan, build_plan
from .scanner import STATUS_FAILED, STATUS_SELECTED, ScanResult

FIXTURE_VERSION = 1


def plan_fixture(
    name: str,
    source: str,
    *,
    as_of: date,
    scan: ScanResult,
    holdings: dict[str, float],
    prices: dict[str, float | None],
    cash: float,
    contribution: float,
    cost_basis: dict[str, float],
    settings: PlanSettings,
    top_n: int,
    cash_etf: str | None,
    currency: str,
    names: dict[str, str] | None,
    plan: MonthlyPlan,
) -> dict[str, Any]:
    names = names or {}
    picks = list(scan.eligible.index)
    scored = set(picks) | set(holdings)
    relevant = sorted(scored | ({cash_etf} if cash_etf else set()))
    return {
        "version": FIXTURE_VERSION,
        "name": name,
        "source": source,
        "asOf": as_of.isoformat(),
        "currency": currency,
        "topN": top_n,
        "cashInstrument": cash_etf,
        "settings": {
            "minTrade": settings.min_trade,
            "trimBand": settings.trim_band,
            "fractionalShares": settings.fractional_shares,
            "weakMarketInvestPct": settings.weak_market_invest_pct,
        },
        "decision": {
            "marketOk": bool(scan.market_ok),
            "investFraction": 1.0 if scan.market_ok else settings.weak_market_invest_pct,
            "targets": [{"key": key, "weight": 1.0 / top_n} for key in picks],
            "scores": [
                {
                    "key": key,
                    "rank": _int_or_none(row.get("Rank")),
                    "momentum": _float_or_none(row.get("Momentum")),
                    "status": str(row.get("Status", "")),
                    "reason": str(render(row.get("Reason", ""))),
                }
                for key, row in scan.table.iterrows()
                if key in scored
            ],
        },
        "contribution": contribution,
        "portfolio": {
            "cash": cash,
            "holdings": [{"key": key, "quantity": quantity} for key, quantity in holdings.items()],
            "costBasis": {key: cost_basis[key] for key in sorted(cost_basis)},
        },
        "prices": {key: _float_or_none(prices.get(key)) for key in relevant},
        "names": {key: names[key] for key in relevant if key in names},
        "expected": {
            "orders": [
                {
                    "action": order.action,
                    "kind": order.kind,
                    "ticker": order.ticker,
                    "name": order.name,
                    "amount": order.amount,
                    "shares": order.shares,
                    "price": order.price,
                    "reason": render(order.reason),
                }
                for order in plan.orders
            ],
            "totalBefore": plan.total_before,
            "totalAfter": plan.total_after,
            "parkedBefore": plan.parked_before,
            "parkedAfter": plan.parked_after,
            "allocation": [
                {
                    "ticker": row.ticker,
                    "role": row.role,
                    "valueBefore": row.value_before,
                    "valueAfter": row.value_after,
                    "weightBefore": row.weight_before,
                    "weightAfter": row.weight_after,
                    "targetWeight": row.target_weight,
                }
                for row in plan.allocation
            ],
            "notes": render_deep(plan.notes),
        },
    }


def replay(fixture: dict[str, Any]) -> MonthlyPlan:
    """Run plan.py on the inputs stored in a fixture."""
    decision = fixture["decision"]
    table = pd.DataFrame(
        [
            {
                "ETF": score["key"],
                "Momentum": np.nan if score["momentum"] is None else score["momentum"],
                "Rank": np.nan if score["rank"] is None else score["rank"],
                "Status": score["status"],
                "Reason": score["reason"],
            }
            for score in decision["scores"]
        ],
        columns=["ETF", "Momentum", "Rank", "Status", "Reason"],
    ).set_index("ETF")
    picks = [target["key"] for target in decision["targets"]]
    scan = ScanResult(
        market_ok=decision["marketOk"],
        spy_above_ma=decision["marketOk"],
        vix=None,
        table=table,
        eligible=table.loc[picks],
    )
    settings = fixture["settings"]
    portfolio = fixture["portfolio"]
    return build_plan(
        scan,
        holdings={holding["key"]: holding["quantity"] for holding in portfolio["holdings"]},
        prices=fixture["prices"],
        cash=portfolio["cash"],
        settings=PlanSettings(
            monthly_contribution=fixture["contribution"],
            weak_market_invest_pct=settings["weakMarketInvestPct"],
            trim_band=settings["trimBand"],
            min_trade=settings["minTrade"],
            fractional_shares=settings["fractionalShares"],
        ),
        top_n=fixture["topN"],
        cash_etf=fixture["cashInstrument"],
        names=fixture.get("names"),
        currency=fixture["currency"],
        contribution=fixture["contribution"],
        cost_basis=portfolio["costBasis"],
    )


def backtest_cases(prices: pd.DataFrame, config: StrategyConfig, *, months: int, label: str) -> list[dict[str, Any]]:
    """Fixtures for the last *months* monthly plans of a backtest on *prices*."""
    captures: list[PlanCapture] = []
    run_backtest(prices, config, years=math.ceil(months / 12) + 1, plan_observer=captures.append)
    return [
        plan_fixture(
            f"{label}-{capture.day:%Y-%m}",
            "backtest",
            as_of=capture.day.date(),
            scan=capture.scan,
            holdings=capture.holdings,
            prices=capture.prices,
            cash=capture.cash,
            contribution=capture.contribution,
            cost_basis=capture.cost_basis,
            settings=config.plan,
            top_n=config.scanner.top_n,
            cash_etf=config.cash_etf,
            currency=config.currency,
            names=config.names,
            plan=capture.plan,
        )
        for capture in captures[-months:]
    ]


# --- Synthetic scenarios: one per rule of plan.py ------------------------------------------

_PRICES = {"AAA": 10.0, "BBB": 20.0, "CCC": 50.0, "DDD": 25.0, "OLD": 40.0, "CASHX": 100.0}
_AS_OF = date(2026, 10, 5)


def _synthetic_scan(picks: list[str], market_ok: bool = True, others: dict[str, str] | None = None) -> ScanResult:
    rows = [
        {"ETF": ticker, "Momentum": 0.3 - rank / 100, "Rank": rank, "Status": STATUS_SELECTED, "Reason": msg("etf_momentum.scan.ranked", rank=rank)}
        for rank, ticker in enumerate(picks, 1)
    ]
    rows += [{"ETF": ticker, "Momentum": -0.1, "Rank": np.nan, "Status": STATUS_FAILED, "Reason": reason} for ticker, reason in (others or {}).items()]
    table = pd.DataFrame(rows, columns=["ETF", "Momentum", "Rank", "Status", "Reason"]).set_index("ETF")
    return ScanResult(market_ok=market_ok, spy_above_ma=market_ok, vix=18.0, table=table, eligible=table.loc[picks])


def _synthetic(
    name: str,
    picks: list[str],
    *,
    market_ok: bool = True,
    others: dict[str, str] | None = None,
    holdings: dict[str, float] | None = None,
    cost_basis: dict[str, float] | None = None,
    cash: float = 0.0,
    contribution: float = 400.0,
    prices: dict[str, float | None] | None = None,
    top_n: int = 4,
    settings: PlanSettings | None = None,
) -> dict[str, Any]:
    scan = _synthetic_scan(picks, market_ok, others)
    holdings = holdings or {}
    cost_basis = cost_basis or {}
    prices = prices or _PRICES
    settings = settings or PlanSettings(monthly_contribution=contribution, min_trade=25.0)
    plan = build_plan(
        scan,
        holdings=holdings,
        prices=prices,
        cash=cash,
        settings=settings,
        top_n=top_n,
        cash_etf="CASHX",
        contribution=contribution,
        cost_basis=cost_basis,
    )
    return plan_fixture(
        name,
        "synthetic",
        as_of=_AS_OF,
        scan=scan,
        holdings=holdings,
        prices=prices,
        cash=cash,
        contribution=contribution,
        cost_basis=cost_basis,
        settings=settings,
        top_n=top_n,
        cash_etf="CASHX",
        currency="EUR",
        names=None,
        plan=plan,
    )


def synthetic_cases() -> list[dict[str, Any]]:
    return [
        _synthetic("new-money-split-equally", ["AAA", "BBB", "CCC", "DDD"]),
        _synthetic("unfilled-slots-are-parked", ["AAA", "BBB"]),
        _synthetic("weak-market-invests-part-and-parks-rest", ["AAA", "BBB", "CCC", "DDD"], market_ok=False),
        _synthetic(
            "weak-market-keeps-parked-cash-and-holdings",
            ["AAA", "BBB"],
            market_ok=False,
            holdings={"AAA": 10.0, "CASHX": 3.0},
            cost_basis={"AAA": 8.0, "CASHX": 99.0},
        ),
        _synthetic(
            "exit-position-that-dropped-out",
            ["AAA", "BBB"],
            others={"OLD": t("etf_momentum.scan.below_ma")},
            holdings={"OLD": 5.0, "AAA": 10.0},
            cost_basis={"OLD": 35.0, "AAA": 9.0},
        ),
        _synthetic(
            "exit-position-outside-strategy-list",
            ["AAA"],
            holdings={"OLD": 2.0},
            cost_basis={"OLD": 30.0},
        ),
        _synthetic(
            "trim-winner-far-above-target",
            ["AAA", "BBB", "CCC", "DDD"],
            holdings={"AAA": 100.0, "BBB": 5.0, "CCC": 2.0, "DDD": 4.0},
            cost_basis={"AAA": 5.0, "BBB": 20.0, "CCC": 50.0, "DDD": 25.0},
            contribution=100.0,
        ),
        _synthetic(
            "no-trim-for-losing-position",
            ["AAA", "BBB", "CCC", "DDD"],
            holdings={"AAA": 100.0, "BBB": 5.0, "CCC": 2.0, "DDD": 4.0},
            cost_basis={"AAA": 12.0, "BBB": 20.0, "CCC": 50.0, "DDD": 25.0},
            contribution=100.0,
        ),
        _synthetic(
            "unpark-cash-etf-when-market-recovers",
            ["AAA", "BBB"],
            holdings={"CASHX": 5.0},
            cost_basis={"CASHX": 98.0},
            contribution=100.0,
        ),
        _synthetic(
            "whole-shares-only",
            ["AAA", "BBB", "CCC"],
            settings=PlanSettings(monthly_contribution=400.0, min_trade=25.0, fractional_shares=False),
        ),
        _synthetic(
            "whole-shares-unpark-rounds-up",
            ["AAA", "BBB"],
            holdings={"CASHX": 5.0},
            cost_basis={"CASHX": 98.0},
            contribution=130.0,
            settings=PlanSettings(monthly_contribution=130.0, min_trade=25.0, fractional_shares=False),
        ),
        _synthetic(
            "small-buys-below-min-trade-are-skipped",
            ["AAA", "BBB", "CCC", "DDD"],
            holdings={"AAA": 9.0, "BBB": 4.6, "CCC": 1.9, "DDD": 3.0},
            cost_basis={"AAA": 9.0, "BBB": 19.0, "CCC": 49.0, "DDD": 24.0},
            contribution=60.0,
        ),
        _synthetic("no-picks-park-everything", [], contribution=227.0),
        _synthetic(
            "holding-without-price-is-flagged",
            ["AAA", "BBB"],
            holdings={"OLD": 3.0, "AAA": 4.0},
            cost_basis={"OLD": 40.0, "AAA": 10.0},
            prices={**_PRICES, "OLD": None},
        ),
        _synthetic("zero-contribution", ["AAA", "BBB"], holdings={"AAA": 10.0, "BBB": 5.0}, cost_basis={"AAA": 9.0, "BBB": 19.0}, contribution=0.0),
        _synthetic(
            "existing-cash-is-invested",
            ["AAA", "BBB", "CCC", "DDD"],
            cash=120.0,
            contribution=227.0,
        ),
        _synthetic("three-slot-portfolio", ["AAA", "BBB"], top_n=3, contribution=300.0),
    ]


def _float_or_none(value: Any) -> float | None:
    return None if value is None or pd.isna(value) else float(value)


def _int_or_none(value: Any) -> int | None:
    return None if value is None or pd.isna(value) else int(value)
