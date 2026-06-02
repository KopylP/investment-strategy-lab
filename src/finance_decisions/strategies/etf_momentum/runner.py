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
    
    telegram_text = _format_scanner_telegram_message(result, current_positions)
    send_telegram(
        telegram_text,
        os.environ.get("TELEGRAM_TOKEN"),
        os.environ.get("TELEGRAM_CHAT_ID"),
    )
    return 0


def _format_scanner_telegram_message(result: Any, current_positions: list[str]) -> str:
    spy_status = "так" if result.spy_above_ma else "ні"
    vix_str = f"{result.vix:.1f}" if result.vix is not None else "недоступний"
    market_status = "так" if result.market_ok else "ні"
    
    lines = [
        "Місячний ETF-сканер",
        "",
        f"SPY вище MA200: {spy_status}",
        f"VIX: {vix_str}",
        f"Стан ринку OK: {market_status}",
        "",
    ]
    
    if not result.market_ok:
        lines.append("Ринкові умови несприятливі. Рекомендація: тримати кеш.")
        return "\n".join(lines)
        
    lines.append(f"Топ-{len(result.eligible)} кандидатів:")
    if result.eligible.empty:
        lines.append("- Немає кандидатів. Тримати кеш.")
    else:
        for ticker, row in result.eligible.iterrows():
            lines.append(
                f"- {ticker}: моментум {row['Momentum 6M'] * 100:+.1f}%, "
                f"Sharpe {row['Sharpe']:.2f}, просадка {row['Drawdown'] * 100:+.1f}%"
            )
            
    if current_positions:
        to_buy = [ticker for ticker in result.eligible.index if ticker not in current_positions]
        to_sell = [ticker for ticker in current_positions if ticker not in result.eligible.index]
        to_hold = [ticker for ticker in current_positions if ticker in result.eligible.index]
        
        to_buy_str = ", ".join(to_buy) if to_buy else "-"
        to_sell_str = ", ".join(to_sell) if to_sell else "-"
        to_hold_str = ", ".join(to_hold) if to_hold else "-"
        
        lines.extend([
            "",
            "Зміни в портфелі:",
            f"- Купити: {to_buy_str}",
            f"- Продати: {to_sell_str}",
            f"- Утримувати: {to_hold_str}",
        ])
        
    return "\n".join(lines)
