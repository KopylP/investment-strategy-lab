# ETF Momentum Strategy

ETF Momentum is the first active strategy in this project. It ranks ETFs for
monthly candidate selection and monitors current positions for weekly risk exits.

## Package Layout

```text
src/finance_decisions/strategies/etf_momentum/
├── config/
│   ├── strategy.toml
│   └── strategy_us.toml
├── templates/
│   ├── monthly_scanner.html
│   └── weekly_risk_monitor.html
├── config.py
├── reports.py
├── risk.py
├── scanner.py
├── README.md
├── monthly_scanner.py
├── positions.json
├── runner.py
└── weekly_risk_monitor.py
```

- `config/` holds defaults for the ETF momentum strategy (European and US versions).
- `templates/` contains Jinja2 HTML templates for strategy reports.
- `config.py` implements configuration loaders and default ticker lists.
- `reports.py` converts analysis results to template payload objects and registers custom filters.
- `risk.py` implements trailing drawdown, volatility stops, and portfolio risk checks.
- `scanner.py` ranks candidate ETFs using Sharpe ratio and lookback momentum rules.
- `positions.json` is the current portfolio source for this strategy.
- `monthly_scanner.py` and `weekly_risk_monitor.py` are the CLI entrypoints.
- `runner.py` orchestrates downloading, analysis, report generation, and Telegram notifications.

Shared infrastructure stays outside the strategy package in `src/finance_decisions/`.

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
