from __future__ import annotations

import json

import numpy as np
import pandas as pd

from finance_decisions.models import Position
from finance_decisions.strategies.etf_momentum.backtest import run_backtest
from finance_decisions.strategies.etf_momentum.config import (
    BacktestSettings,
    PlanSettings,
    RiskSettings,
    ScannerSettings,
    StrategyConfig,
)
from finance_decisions.strategies.etf_momentum.plan import build_plan
from finance_decisions.strategies.etf_momentum.reports import (
    format_monthly_telegram,
    format_scan_report,
    write_backtest_outputs,
    write_risk_outputs,
    write_scanner_outputs,
)
from finance_decisions.strategies.etf_momentum.risk import analyze_portfolio_risk
from finance_decisions.strategies.etf_momentum.scanner import scan_etfs


def _prices(periods: int = 420) -> pd.DataFrame:
    dates = pd.date_range("2024-01-01", periods=periods, freq="B")
    rng = np.random.default_rng(7)
    frame = {
        "SPY": 100 * np.cumprod(1 + rng.normal(0.0006, 0.008, periods)),
        "^VIX": np.full(periods, 17.0),
        "CASHX": np.linspace(100, 106, periods),
    }
    for name, drift in {"AAA": 0.0012, "BBB": 0.0009, "CCC": 0.0007, "DDD": -0.0004, "OLD": -0.0008}.items():
        frame[name] = 50 * np.cumprod(1 + rng.normal(drift, 0.007, periods))
    return pd.DataFrame(frame, index=dates)


def _scan_and_plan(prices: pd.DataFrame, market_ok: bool = True):
    if not market_ok:
        prices = prices.copy()
        prices["^VIX"] = 40.0
    settings = ScannerSettings(top_n=3, lookback_months=3, ma_period=50)
    scan = scan_etfs(prices, settings, ["AAA", "BBB", "CCC", "DDD", "OLD"], ["OLD"], names={"AAA": "Alpha ETF"}, cash_etf="CASHX")
    plan = build_plan(
        scan,
        holdings={"OLD": 5.0},
        prices={t: float(prices[t].iloc[-1]) for t in prices.columns},
        cash=0.0,
        settings=PlanSettings(monthly_contribution=300.0),
        top_n=3,
        cash_etf="CASHX",
        names={"AAA": "Alpha ETF"},
    )
    return scan, plan


def test_monthly_outputs_share_one_plan(tmp_path) -> None:
    scan, plan = _scan_and_plan(_prices())

    text = write_scanner_outputs(tmp_path, scan, plan, current_positions=["OLD"])
    html = (tmp_path / "report.html").read_text(encoding="utf-8")
    payload = json.loads((tmp_path / "report.json").read_text(encoding="utf-8"))
    telegram = format_monthly_telegram(scan, plan)

    assert "План на" in html
    assert "Що зробити" in html
    assert "Портфель після угод" in html
    assert "Чому саме ці ETF" in html
    assert "Як виконати угоди" in html
    assert "Alpha ETF" in html
    assert payload["plan"]["orders"][0]["ticker"] == "OLD"
    # Every channel lists the same orders in the same order.
    for index, order in enumerate(plan.orders, 1):
        line = f"{index}. {'Продати' if order.action == 'sell' else 'Купити'} {order.ticker}"
        assert line in text
        assert line in telegram
        assert order.ticker in html


def test_weak_market_message_is_identical_everywhere(tmp_path) -> None:
    scan, plan = _scan_and_plan(_prices(), market_ok=False)

    write_scanner_outputs(tmp_path, scan, plan, current_positions=["OLD"])
    html = (tmp_path / "report.html").read_text(encoding="utf-8")
    text = format_scan_report(scan, plan)
    telegram = format_monthly_telegram(scan, plan)

    assert scan.market_ok is False
    for output in (text, telegram):
        assert "Ринок слабкий: інвестуємо 50% внеску" in output
        assert "тримати кеш" not in output.lower()
    assert "Ринок слабкий. Інвестуємо 50% внеску" in html
    assert "25-50%" not in html


def test_write_risk_outputs_renders_html(tmp_path) -> None:
    prices = _prices()
    report = analyze_portfolio_risk(prices, [Position("AAA", 40.0, 3, "2024-06-03", "EUR")], RiskSettings())

    write_risk_outputs(tmp_path, report)

    html = (tmp_path / "report.html").read_text(encoding="utf-8")
    assert "Перевірка ризиків" in html
    assert "Позиції" in html
    assert "Вихід, якщо ціна закриття" in html
    assert "Правила виходу" in html


def test_backtest_runs_and_renders(tmp_path) -> None:
    prices = _prices(periods=900)
    prices["BENCH"] = prices["SPY"]
    config = StrategyConfig(
        etfs=["AAA", "BBB", "CCC", "DDD", "OLD"],
        risk=RiskSettings(),
        scanner=ScannerSettings(top_n=2, lookback_months=3, ma_period=50),
        plan=PlanSettings(monthly_contribution=500.0),
        backtest=BacktestSettings(years=3, benchmark="BENCH"),
        cash_etf="CASHX",
    )

    result = run_backtest(prices, config, initial=1000.0)
    write_backtest_outputs(tmp_path, result)

    assert result.months >= 20
    assert result.metrics["strategy"]["contributed"] == 1000.0 + 500.0 * result.months
    assert result.trades > 0
    assert result.growth.iloc[0] == 1.0
    html = (tmp_path / "report.html").read_text(encoding="utf-8")
    assert "Бектест стратегії" in html
    assert "<polyline" in html
