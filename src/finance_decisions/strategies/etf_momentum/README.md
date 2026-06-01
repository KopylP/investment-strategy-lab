# ETF Momentum Strategy

ETF Momentum is the first active strategy in this project. It ranks ETFs for
monthly candidate selection and monitors current positions for weekly risk exits.

## Package Layout

```text
src/finance_decisions/strategies/etf_momentum/
├── config/
│   └── strategy.toml
├── README.md
├── monthly_scanner.py
├── positions.json
├── runner.py
└── weekly_risk_monitor.py
```

- `config/strategy.toml` owns ETF universe and strategy thresholds.
- `positions.json` is the current portfolio source for this strategy.
- `monthly_scanner.py` is the module entrypoint for ETF ranking.
- `weekly_risk_monitor.py` is the module entrypoint for risk checks.
- `runner.py` orchestrates shared infrastructure for both entrypoints.

Shared infrastructure stays outside the strategy package in
`src/finance_decisions/`.

## Run Locally

From the repository root:

```bash
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.weekly_risk_monitor
```

Reports are written to:

```text
reports/latest/etf-momentum/
```

## Configuration

Edit `config/strategy.toml` for normal strategy changes. GitHub Actions can
override selected config values through repository variables.

## Positions

Update `positions.json` after every trade. Keep the repository private if this
file contains real holdings.
