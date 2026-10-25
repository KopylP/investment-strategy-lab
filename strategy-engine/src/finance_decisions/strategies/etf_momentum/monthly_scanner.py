from __future__ import annotations

import argparse
from pathlib import Path

from finance_decisions.strategies.etf_momentum.runner import (
    DEFAULT_MONTHLY_REPORT_DIR,
    DEFAULT_POSITIONS,
    run_monthly_scanner,
)


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the monthly scanner for ETF momentum strategy.")
    parser.add_argument(
        "--config",
        type=Path,
        default=None,
        help="Path to strategy config TOML file (falls back to STRATEGY_CONFIG env var).",
    )
    parser.add_argument(
        "--positions",
        type=Path,
        default=DEFAULT_POSITIONS,
        help="Path to positions JSON file.",
    )
    parser.add_argument(
        "--report-dir",
        type=Path,
        default=DEFAULT_MONTHLY_REPORT_DIR,
        help="Directory to write output reports.",
    )
    parser.add_argument(
        "--contribution",
        type=float,
        default=None,
        help="New money to invest this month (overrides plan.monthly_contribution in the config).",
    )
    args = parser.parse_args()

    return run_monthly_scanner(
        config_path=args.config,
        positions_path=args.positions,
        report_dir=args.report_dir,
        contribution=args.contribution,
    )


if __name__ == "__main__":
    raise SystemExit(main())
