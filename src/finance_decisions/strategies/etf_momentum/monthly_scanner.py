from __future__ import annotations

from finance_decisions.strategies.etf_momentum.runner import run_monthly_scanner


def main() -> int:
    return run_monthly_scanner()


if __name__ == "__main__":
    raise SystemExit(main())
