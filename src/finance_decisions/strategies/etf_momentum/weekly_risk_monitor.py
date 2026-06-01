from __future__ import annotations

from finance_decisions.strategies.etf_momentum.runner import run_weekly_risk_monitor


def main() -> int:
    return run_weekly_risk_monitor()


if __name__ == "__main__":
    raise SystemExit(main())
