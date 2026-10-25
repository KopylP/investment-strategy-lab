from __future__ import annotations

import logging
import os
from pathlib import Path

from finance_decisions.helpers import holdings_by_value, latest_value
from finance_decisions.market_data import download_close_prices
from finance_decisions.models import Portfolio, load_portfolio
from finance_decisions.notifications import send_telegram

# Local strategy imports
from .config import load_strategy_config
from .plan import build_plan
from .reports import (
    format_monthly_telegram,
    format_weekly_telegram,
    write_risk_outputs,
    write_scanner_outputs,
)
from .risk import analyze_portfolio_risk
from .scanner import scan_etfs

STRATEGY_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG = STRATEGY_DIR / "config" / "strategy.toml"
DEFAULT_POSITIONS = STRATEGY_DIR / "positions.json"
REPORTS_ROOT = STRATEGY_DIR.parents[3] / "reports" / "latest" / "etf-momentum"
DEFAULT_MONTHLY_REPORT_DIR = REPORTS_ROOT / "monthly-scanner"
DEFAULT_WEEKLY_REPORT_DIR = REPORTS_ROOT / "weekly-risk-monitor"
DEFAULT_BACKTEST_REPORT_DIR = REPORTS_ROOT / "backtest"

# Configure logging
logging.basicConfig(level=logging.INFO, format="%(message)s")
logger = logging.getLogger(__name__)


def run_weekly_risk_monitor(
    *,
    config_path: Path | None = None,
    positions_path: Path = DEFAULT_POSITIONS,
    report_dir: Path = DEFAULT_WEEKLY_REPORT_DIR,
) -> int:
    config = load_strategy_config(config_path)
    portfolio = load_portfolio(positions_path)

    tickers = sorted(set(portfolio.tickers) | {"SPY", "^VIX"})
    prices = download_close_prices(tickers, period="2y")
    report = analyze_portfolio_risk(
        prices,
        portfolio.lots,
        settings=config.risk,
        cash=portfolio.cash,
        cash_etf=config.cash_etf,
        names=config.names,
    )
    text = write_risk_outputs(report_dir, report)

    logger.info(text)
    logger.info(f"\nReports written to: {report_dir}")
    _notify(format_weekly_telegram(report))
    return 0


def run_monthly_scanner(
    *,
    config_path: Path | None = None,
    positions_path: Path = DEFAULT_POSITIONS,
    report_dir: Path = DEFAULT_MONTHLY_REPORT_DIR,
    contribution: float | None = None,
) -> int:
    config = load_strategy_config(config_path)
    portfolio = load_portfolio(positions_path) if positions_path.exists() else Portfolio()
    holdings = {position.ticker: position.shares for position in portfolio.positions}

    tickers = set(config.etfs) | set(holdings) | {"SPY", "^VIX"}
    if config.cash_etf:
        tickers.add(config.cash_etf)
    prices = download_close_prices(sorted(tickers), period="3y")

    latest = {ticker: latest_value(prices[ticker]) for ticker in prices.columns}
    result = scan_etfs(
        prices,
        settings=config.scanner,
        etfs=config.etfs,
        current_positions=holdings_by_value(holdings, latest),
        groups=config.groups,
        names=config.names,
        cash_etf=config.cash_etf,
    )
    plan = build_plan(
        result,
        holdings=holdings,
        prices=latest,
        cash=portfolio.cash,
        settings=config.plan,
        top_n=config.scanner.top_n,
        cash_etf=config.cash_etf,
        names=config.names,
        currency=config.currency,
        contribution=contribution,
        cost_basis={position.ticker: position.entry_price for position in portfolio.positions},
    )

    text = write_scanner_outputs(report_dir, result, plan, current_positions=list(holdings))
    logger.info(text)
    logger.info(f"\nReports written to: {report_dir}")
    _notify(format_monthly_telegram(result, plan))
    return 0


def _notify(text: str) -> None:
    send_telegram(text, os.environ.get("TELEGRAM_TOKEN"), os.environ.get("TELEGRAM_CHAT_ID"))
