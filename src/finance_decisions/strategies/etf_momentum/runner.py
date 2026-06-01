from __future__ import annotations

import os
from pathlib import Path

from finance_decisions.config import load_strategy_config
from finance_decisions.market_data import download_close_prices
from finance_decisions.models import load_positions
from finance_decisions.notifications import send_telegram
from finance_decisions.reports import write_risk_outputs, write_scanner_outputs
from finance_decisions.risk import analyze_portfolio_risk
from finance_decisions.scanner import scan_etfs

STRATEGY_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG = STRATEGY_DIR / "config" / "strategy.toml"
DEFAULT_POSITIONS = STRATEGY_DIR / "positions.json"
DEFAULT_MONTHLY_REPORT_DIR = "reports/latest/etf-momentum/monthly-scanner"
DEFAULT_WEEKLY_REPORT_DIR = "reports/latest/etf-momentum/weekly-risk-monitor"


def run_weekly_risk_monitor(
    *,
    default_config: str | Path = DEFAULT_CONFIG,
    default_positions: str | Path = DEFAULT_POSITIONS,
    default_report_dir: str = DEFAULT_WEEKLY_REPORT_DIR,
) -> int:
    config_path = Path(os.environ.get("STRATEGY_CONFIG", default_config))
    positions_path = Path(os.environ.get("POSITIONS_FILE", default_positions))
    report_dir = Path(os.environ.get("REPORT_DIR", default_report_dir))

    config = load_strategy_config(config_path)
    positions = load_positions(positions_path)

    tickers = sorted({position.ticker for position in positions} | {"SPY", "^VIX"})
    prices = download_close_prices(tickers, period="1y")
    report = analyze_portfolio_risk(prices, positions, settings=config.risk)
    text = write_risk_outputs(report_dir, report)

    print(text)
    print(f"\nReports written to: {report_dir}")
    send_telegram(
        text,
        os.environ.get("TELEGRAM_TOKEN"),
        os.environ.get("TELEGRAM_CHAT_ID"),
    )
    return 0


def run_monthly_scanner(
    *,
    default_config: str | Path = DEFAULT_CONFIG,
    default_positions: str | Path = DEFAULT_POSITIONS,
    default_report_dir: str = DEFAULT_MONTHLY_REPORT_DIR,
) -> int:
    config_path = Path(os.environ.get("STRATEGY_CONFIG", default_config))
    positions_path = Path(os.environ.get("POSITIONS_FILE", default_positions))
    report_dir = Path(os.environ.get("REPORT_DIR", default_report_dir))

    config = load_strategy_config(config_path)
    tickers = sorted(set(config.etfs + ["SPY", "^VIX"]))
    prices = download_close_prices(tickers, period="3y")
    result = scan_etfs(prices, settings=config.scanner)

    current_positions = []
    if positions_path.exists():
        current_positions = [position.ticker for position in load_positions(positions_path)]

    text = write_scanner_outputs(report_dir, result, current_positions=current_positions)
    print(text)
    print(f"\nReports written to: {report_dir}")
    send_telegram(
        text,
        os.environ.get("TELEGRAM_TOKEN"),
        os.environ.get("TELEGRAM_CHAT_ID"),
    )
    return 0
