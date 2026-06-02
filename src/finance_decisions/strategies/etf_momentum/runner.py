from __future__ import annotations

import logging
import os
from pathlib import Path

from finance_decisions.market_data import download_close_prices
from finance_decisions.models import load_positions
from finance_decisions.notifications import send_telegram

# Local strategy imports
from .config import load_strategy_config
from .reports import write_risk_outputs, write_scanner_outputs
from .risk import analyze_portfolio_risk
from .scanner import scan_etfs

STRATEGY_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG = STRATEGY_DIR / "config" / "strategy.toml"
DEFAULT_POSITIONS = STRATEGY_DIR / "positions.json"
DEFAULT_MONTHLY_REPORT_DIR = STRATEGY_DIR.parents[3] / "reports" / "latest" / "etf-momentum" / "monthly-scanner"
DEFAULT_WEEKLY_REPORT_DIR = STRATEGY_DIR.parents[3] / "reports" / "latest" / "etf-momentum" / "weekly-risk-monitor"

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
    positions = load_positions(positions_path)

    tickers = sorted({position.ticker for position in positions} | {"SPY", "^VIX"})
    prices = download_close_prices(tickers, period="1y")
    report = analyze_portfolio_risk(prices, positions, settings=config.risk)
    text = write_risk_outputs(report_dir, report)

    logger.info(text)
    logger.info(f"\nReports written to: {report_dir}")
    send_telegram(
        text,
        os.environ.get("TELEGRAM_TOKEN"),
        os.environ.get("TELEGRAM_CHAT_ID"),
    )
    return 0


def run_monthly_scanner(
    *,
    config_path: Path | None = None,
    positions_path: Path = DEFAULT_POSITIONS,
    report_dir: Path = DEFAULT_MONTHLY_REPORT_DIR,
) -> int:
    config = load_strategy_config(config_path)
    tickers = sorted(set(config.etfs + ["SPY", "^VIX"]))
    prices = download_close_prices(tickers, period="3y")

    current_positions = []
    if positions_path.exists():
        current_positions = [position.ticker for position in load_positions(positions_path)]

    result = scan_etfs(
        prices,
        settings=config.scanner,
        etfs=config.etfs,
        current_positions=current_positions,
    )

    text = write_scanner_outputs(report_dir, result, current_positions=current_positions)
    logger.info(text)
    logger.info(f"\nReports written to: {report_dir}")
    send_telegram(
        text,
        os.environ.get("TELEGRAM_TOKEN"),
        os.environ.get("TELEGRAM_CHAT_ID"),
    )
    return 0
