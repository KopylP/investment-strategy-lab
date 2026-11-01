"""Replay the strategy on history with the same code that produces the live reports.

Each month: scan on data up to the previous close, build the plan, execute the orders
at the next close with trading costs. Each week in between: run the risk monitor and
sell positions it flags. The benchmark receives the same contributions into a single ETF.

Caveats: the ETF list is today's list (survivorship bias), Yahoo prices are adjusted
for distributions, and taxes are ignored.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass, field
from datetime import date
import logging
from pathlib import Path
from typing import Callable

import numpy as np
import pandas as pd

from finance_decisions.market_data import BENCHMARK_SYMBOL, MARKET_SYMBOLS, download_close_prices
from finance_decisions.models import Position, aggregate_positions
from .config import StrategyConfig, load_strategy_config
from .plan import MonthlyPlan, build_plan
from .risk import analyze_portfolio_risk
from .scanner import ScanResult, scan_etfs

logger = logging.getLogger(__name__)

WARMUP_DAYS = 260
SCAN_WINDOW = 800
RISK_WINDOW = 300


@dataclass(frozen=True)
class BacktestResult:
    start: date
    end: date
    contribution: float
    initial: float
    equity: pd.Series
    growth: pd.Series
    benchmark_equity: pd.Series
    benchmark_growth: pd.Series
    contributed: pd.Series
    benchmark: str
    trades: int
    costs: float
    risk_exits: int
    months: int
    months_market_ok: int
    metrics: dict[str, dict[str, float]]
    yearly: list[dict]
    picks_log: list[dict] = field(default_factory=list)


@dataclass(frozen=True)
class PlanCapture:
    """Inputs and output of one monthly ``build_plan`` call (used to export golden fixtures)."""

    day: pd.Timestamp
    scan: ScanResult
    holdings: dict[str, float]
    prices: dict[str, float]
    cash: float
    contribution: float
    cost_basis: dict[str, float]
    plan: MonthlyPlan


class _Book:
    """Lots plus broker cash, with trading costs."""

    def __init__(self, cost_pct: float, fee: float) -> None:
        self.lots: list[Position] = []
        self.cash = 0.0
        self.cost_pct = cost_pct
        self.fee = fee
        self.trades = 0
        self.costs = 0.0

    def holdings(self) -> dict[str, float]:
        return {position.ticker: position.shares for position in aggregate_positions(self.lots)}

    def value(self, prices: pd.Series) -> float:
        return self.cash + sum(shares * float(prices.get(t, 0.0) or 0.0) for t, shares in self.holdings().items())

    def buy(self, ticker: str, amount: float, price: float, day: pd.Timestamp) -> None:
        if not price or pd.isna(price) or amount <= 0:
            return
        affordable = max(0.0, (self.cash - self.fee) / (1 + self.cost_pct))
        amount = min(amount, affordable)
        if amount <= 0:
            return
        cost = amount * self.cost_pct + self.fee
        self.cash -= amount + cost
        self.costs += cost
        self.trades += 1
        self.lots.append(Position(ticker, float(price), amount / float(price), day.date().isoformat(), "EUR"))

    def sell(self, ticker: str, shares: float, price: float) -> None:
        held = sum(lot.shares for lot in self.lots if lot.ticker == ticker)
        if not price or pd.isna(price) or held <= 0:
            return
        shares = min(shares, held)
        ratio = 1 - shares / held
        kept = []
        for lot in self.lots:
            if lot.ticker != ticker:
                kept.append(lot)
            elif ratio > 1e-9:
                kept.append(Position(lot.ticker, lot.entry_price, lot.shares * ratio, lot.entry_date, lot.currency))
        self.lots = kept
        amount = shares * float(price)
        cost = amount * self.cost_pct + self.fee
        self.cash += amount - cost
        self.costs += cost
        self.trades += 1


def run_backtest(
    prices: pd.DataFrame,
    config: StrategyConfig,
    *,
    contribution: float | None = None,
    initial: float = 0.0,
    years: int | None = None,
    plan_observer: Callable[[PlanCapture], None] | None = None,
) -> BacktestResult:
    contribution = config.plan.monthly_contribution if contribution is None else contribution
    years = years or config.backtest.years
    benchmark = config.backtest.benchmark
    if not benchmark:
        raise ValueError("the strategy config names no backtest benchmark")
    prices = prices.sort_index().dropna(how="all")
    valuation = prices.ffill()

    spy_obs = prices[BENCHMARK_SYMBOL].notna().cumsum() if BENCHMARK_SYMBOL in prices.columns else pd.Series(0, index=prices.index)
    earliest = prices.index[-1] - pd.DateOffset(years=years)
    days = prices.index[(spy_obs >= WARMUP_DAYS) & (prices.index >= earliest)]
    if len(days) < 40:
        raise ValueError("Not enough history for a backtest.")

    book = _Book(config.backtest.cost_pct, config.backtest.fee_per_trade)
    bench = _Book(config.backtest.cost_pct, config.backtest.fee_per_trade)
    equity, bench_equity, flows, contributed = {}, {}, {}, {}
    total_in = 0.0
    months = months_ok = risk_exits = 0
    picks_log = []

    for position, day in enumerate(days):
        previous = prices.index[prices.index.get_loc(day) - 1]
        today_prices = valuation.loc[day]
        flow = 0.0
        new_month = position == 0 or day.month != days[position - 1].month
        new_week = position > 0 and day.isocalendar()[1] != days[position - 1].isocalendar()[1]

        if new_month:
            history = prices.loc[:previous].tail(SCAN_WINDOW)
            flow = contribution + (initial if position == 0 else 0.0)
            holdings = book.holdings()
            scan = scan_etfs(
                history,
                settings=config.scanner,
                etfs=config.etfs,
                current_positions=sorted(holdings, key=lambda t: holdings[t] * float(today_prices.get(t) or 0.0), reverse=True),
                groups=config.groups,
                names=config.names,
                cash_etf=config.cash_etf,
                now=day,
            )
            costs = {p.ticker: p.entry_price for p in aggregate_positions(book.lots)}
            plan = build_plan(
                scan,
                holdings=holdings,
                prices=today_prices.to_dict(),
                cash=book.cash,
                settings=config.plan,
                top_n=config.scanner.top_n,
                cash_etf=config.cash_etf,
                names=config.names,
                currency=config.currency,
                contribution=flow,
                cost_basis=costs,
            )
            if plan_observer is not None:
                plan_observer(PlanCapture(day, scan, holdings, today_prices.to_dict(), book.cash, flow, costs, plan))
            book.cash += flow
            for order in plan.orders:
                if order.action == "sell":
                    book.sell(order.ticker, order.shares, today_prices.get(order.ticker))
                else:
                    book.buy(order.ticker, order.amount, today_prices.get(order.ticker), day)
            bench.cash += flow
            bench.buy(benchmark, bench.cash, today_prices.get(benchmark), day)
            months += 1
            months_ok += int(scan.market_ok)
            picks_log.append({"date": day.date(), "market_ok": scan.market_ok, "picks": list(scan.eligible.index)})
        elif new_week and book.lots:
            history = prices.loc[:previous].tail(RISK_WINDOW)
            report = analyze_portfolio_risk(history, book.lots, config.risk, cash=book.cash, cash_etf=config.cash_etf)
            to_sell = [r.ticker for r in report.position_risks if r.sell]
            if report.portfolio_exit_hit:
                to_sell = [t for t in book.holdings() if t != config.cash_etf]
            for ticker in to_sell:
                book.sell(ticker, book.holdings().get(ticker, 0.0), today_prices.get(ticker))
                risk_exits += 1
            if to_sell and config.cash_etf:
                book.buy(config.cash_etf, book.cash, today_prices.get(config.cash_etf), day)

        total_in += flow
        flows[day] = flow
        contributed[day] = total_in
        equity[day] = book.value(today_prices)
        bench_equity[day] = bench.value(today_prices)

    equity_s = pd.Series(equity)
    bench_s = pd.Series(bench_equity)
    flows_s = pd.Series(flows)
    growth = _growth_index(equity_s, flows_s)
    bench_growth = _growth_index(bench_s, flows_s)
    rf = config.scanner.risk_free_rate

    return BacktestResult(
        start=days[0].date(),
        end=days[-1].date(),
        contribution=contribution,
        initial=initial,
        equity=equity_s,
        growth=growth,
        benchmark_equity=bench_s,
        benchmark_growth=bench_growth,
        contributed=pd.Series(contributed),
        benchmark=benchmark,
        trades=book.trades,
        costs=book.costs,
        risk_exits=risk_exits,
        months=months,
        months_market_ok=months_ok,
        metrics={
            "strategy": _metrics(growth, equity_s, total_in, rf),
            "benchmark": _metrics(bench_growth, bench_s, total_in, rf),
        },
        yearly=_yearly(growth, bench_growth),
        picks_log=picks_log,
    )


def _growth_index(values: pd.Series, flows: pd.Series) -> pd.Series:
    """Time-weighted growth of 1: contributions do not count as returns."""
    previous = values.shift(1)
    returns = ((values - flows) / previous - 1).where(previous > 0, 0.0).fillna(0.0)
    return (1 + returns).cumprod()


def _metrics(growth: pd.Series, equity: pd.Series, contributed: float, rf: float) -> dict[str, float]:
    years = max((growth.index[-1] - growth.index[0]).days / 365.25, 1e-9)
    daily = growth.pct_change().dropna()
    cagr = float(growth.iloc[-1] ** (1 / years) - 1)
    vol = float(daily.std() * np.sqrt(252)) if len(daily) > 1 else 0.0
    drawdown = growth / growth.cummax() - 1
    return {
        "cagr": cagr,
        "volatility": vol,
        "sharpe": (cagr - rf) / vol if vol > 0 else 0.0,
        "max_drawdown": float(drawdown.min()),
        "final_value": float(equity.iloc[-1]),
        "contributed": contributed,
        "profit": float(equity.iloc[-1] - contributed),
        "total_return": float(growth.iloc[-1] - 1),
    }


def _yearly(growth: pd.Series, bench: pd.Series) -> list[dict]:
    rows = []
    for year in sorted(set(growth.index.year)):
        g = growth[growth.index.year == year]
        b = bench[bench.index.year == year]
        g_start = growth[growth.index < g.index[0]].iloc[-1] if (growth.index < g.index[0]).any() else g.iloc[0]
        b_start = bench[bench.index < b.index[0]].iloc[-1] if (bench.index < b.index[0]).any() else b.iloc[0]
        rows.append(
            {
                "year": int(year),
                "strategy": float(g.iloc[-1] / g_start - 1),
                "benchmark": float(b.iloc[-1] / b_start - 1),
                "partial": bool(len(g) < 240),
            }
        )
    return rows


def main() -> int:
    from .reports import write_backtest_outputs
    from .runner import DEFAULT_BACKTEST_REPORT_DIR

    logging.basicConfig(level=logging.INFO, format="%(message)s")
    parser = argparse.ArgumentParser(description="Backtest the ETF momentum strategy on historical prices.")
    parser.add_argument("--config", type=Path, default=None, help="Path to strategy config TOML file.")
    parser.add_argument("--years", type=int, default=None, help="Years to replay (default: backtest.years in config).")
    parser.add_argument("--contribution", type=float, default=None, help="Monthly contribution (default: plan.monthly_contribution).")
    parser.add_argument("--initial", type=float, default=0.0, help="Lump sum invested on the first day.")
    parser.add_argument("--report-dir", type=Path, default=DEFAULT_BACKTEST_REPORT_DIR, help="Directory to write output reports.")
    args = parser.parse_args()

    config = load_strategy_config(args.config)
    years = args.years or config.backtest.years
    tickers = set(config.etfs) | {*MARKET_SYMBOLS, config.backtest.benchmark}
    if config.cash_etf:
        tickers.add(config.cash_etf)
    prices = download_close_prices(sorted(tickers), period=f"{years + 2}y")
    result = run_backtest(prices, config, contribution=args.contribution, initial=args.initial, years=years)
    text = write_backtest_outputs(args.report_dir, result)
    logger.info(text)
    logger.info(f"\nReports written to: {args.report_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
