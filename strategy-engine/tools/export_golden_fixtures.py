"""Export golden fixtures of plan.py for the .NET GoldenTests.

Synthetic scenarios cover every rule of plan.py; backtest fixtures replay the last months
of the live strategy on real Yahoo prices (needs network).

    uv run python tools/export_golden_fixtures.py                 # both
    uv run python tools/export_golden_fixtures.py --synthetic-only
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from finance_decisions.market_data import download_close_prices
from finance_decisions.strategies.etf_momentum import golden
from finance_decisions.strategies.etf_momentum.config import load_strategy_config

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = REPO_ROOT / "backend" / "tests" / "InvestmentLab.GoldenTests" / "Fixtures"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--months", type=int, default=12, help="Monthly plans to export from the backtest.")
    parser.add_argument("--synthetic-only", action="store_true")
    args = parser.parse_args()

    _write(args.output / "synthetic", golden.synthetic_cases())
    if not args.synthetic_only:
        config = load_strategy_config()
        tickers = set(config.etfs) | {"SPY", "^VIX", config.backtest.benchmark}
        if config.cash_etf:
            tickers.add(config.cash_etf)
        prices = download_close_prices(sorted(tickers), period="5y")
        _write(args.output / "backtest", golden.backtest_cases(prices, config, months=args.months, label="eu"))


def _write(directory: Path, fixtures: list[dict]) -> None:
    directory.mkdir(parents=True, exist_ok=True)
    for stale in directory.glob("*.json"):
        stale.unlink()
    for fixture in fixtures:
        path = directory / f"{fixture['name']}.json"
        path.write_text(json.dumps(fixture, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{len(fixtures)} fixtures written to {directory}")


if __name__ == "__main__":
    main()
