from __future__ import annotations

import argparse
from pathlib import Path

from finance_decisions.strategies.etf_momentum.runner import (
    DEFAULT_POSITIONS,
    DEFAULT_WEEKLY_REPORT_DIR,
    run_weekly_risk_monitor,
)


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the weekly risk monitor for ETF momentum strategy.")
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
        default=DEFAULT_WEEKLY_REPORT_DIR,
        help="Directory to write output reports.",
    )
    args = parser.parse_args()

    return run_weekly_risk_monitor(
        config_path=args.config,
        positions_path=args.positions,
        report_dir=args.report_dir,
    )


if __name__ == "__main__":
    raise SystemExit(main())
