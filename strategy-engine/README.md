# Strategy Engine (Finance Decisions)

> Part of the Investment Strategy Lab monorepo. Run every command below from
> this `strategy-engine/` directory. The `.github/workflows/` folder lives at the
> repository root.

Small Python app for strategy-level finance decision support. The active ETF
momentum strategy provides:

- a monthly ETF momentum scanner that produces a concrete order plan
- a weekly portfolio risk monitor with an exit price for every position
- a backtest that replays the same rules on history

## Project Layout

```text
.
├── ../.github/workflows/
│   ├── strategy-engine.yml
│   ├── monthly_scanner.yml
│   └── weekly_risk_monitor.yml
├── src/finance_decisions/
│   ├── helpers.py
│   ├── market_data.py
│   ├── models.py
│   ├── notifications.py
│   ├── reports.py             # Strategy-agnostic template rendering engine
│   └── strategies/
│       ├── templates/         # Shared base strategy layouts
│       │   └── base.html
│       └── etf_momentum/
│           ├── config.py      # Strategy configuration defaults
│           ├── config/
│           │   ├── strategy.toml
│           │   └── strategy_us.toml
│           ├── templates/     # Strategy specific HTML templates
│           │   ├── backtest.html
│           │   ├── monthly_scanner.html
│           │   └── weekly_risk_monitor.html
│           ├── backtest.py    # Historical replay + CLI
│           ├── plan.py        # Monthly orders from scan + portfolio
│           ├── reports.py     # Text, Telegram, JSON and HTML outputs
│           ├── risk.py        # Strategy risk calculations
│           ├── scanner.py     # Strategy scanner logic
│           ├── README.md
│           ├── monthly_scanner.py
│           ├── positions.json
│           ├── runner.py
│           └── weekly_risk_monitor.py
├── tests/
├── requirements.txt
└── requirements-dev.txt
```

All strategy-specific code, docs, templates, configurations, indicators, and runtime position state live under `src/finance_decisions/strategies/<strategy_name>/`. Shared reusable infrastructure, such as market data downloaders, the generic reports engine, notifications, base layout templates, and position models, stays in `src/finance_decisions/` or `src/finance_decisions/strategies/`.

There are no root-level strategy wrappers. Run strategies through module entrypoints with command-line flags or environment variables.

## HTTP API (strategy engine for the .NET backend)

The Aspire AppHost in `backend/` starts it automatically. To run it alone:

```bash
uv run uvicorn finance_decisions.api.main:app --reload
```

| Endpoint | What |
|---|---|
| `GET /v1/strategies` | Strategy types with the JSON Schema and defaults of their parameters. |
| `POST /v1/strategies/{code}/decide` | Targets (weights, invest fraction, rationale) for one sub-portfolio. Prices come in the request; data after `asOf` is ignored. |
| `POST /v1/strategies/{code}/risk` | Weekly exit checks: per-position status, exit prices, `exit`/`warn` signals. |
| `POST /v1/strategies/{code}/decide/jobs`, `.../risk/jobs` | The same as a background job, for strategies that think for minutes or hours (an AI agent): `202` with a `jobId` at once. |
| `GET /v1/jobs/{jobId}` | `running`, `succeeded` (with `result` — what `/decide` or `/risk` answers) or `failed` (with `error`). Jobs live in memory: after a restart it is `404` and the backend submits the job again. |
| `POST /v1/prices` | Daily `open`, `high`, `low`, `close` and `adjustedClose` from Yahoo Finance for the backend's MarketData module. |
| `GET /health` | Liveness for Aspire. |

The contract lives in [`contracts/v1/openapi.json`](contracts/v1/openapi.json). After changing
`finance_decisions/api/contracts.py`, regenerate it with `uv run python tools/export_contract.py`; CI fails when it is stale.

Golden fixtures for the .NET rebalancer (the port of `plan.py`) are written to
`backend/tests/InvestmentLab.GoldenTests/Fixtures/` by `uv run python tools/export_golden_fixtures.py`
(synthetic scenarios + the last 12 months of the live strategy on real prices).

## Local Setup

```bash
uv sync          # or: python -m venv .venv && source .venv/bin/activate && pip install -r requirements-dev.txt
```

## Run Locally

By default, the strategy runs in **European** mode (loading EUR-denominated ETFs). You can choose between the **European** (EUR) and **US** (USD) markets using the `STRATEGY_MARKET` environment variable (`EU` or `US`):

```bash
# Run in European mode (default)
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.weekly_risk_monitor

# Run with custom command-line overrides
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner --config src/finance_decisions/strategies/etf_momentum/config/strategy_us.toml --positions src/finance_decisions/strategies/etf_momentum/positions.json --report-dir reports/custom-dir

# Run in US mode (using USD-denominated ETFs)
PYTHONPATH=src STRATEGY_MARKET=US python -m finance_decisions.strategies.etf_momentum.monthly_scanner
PYTHONPATH=src STRATEGY_MARKET=US python -m finance_decisions.strategies.etf_momentum.weekly_risk_monitor

# Plan with a different contribution this month
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner --contribution 600

# Replay the strategy on 8 years of history
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.backtest --years 8

# Run tests
uv run pytest -q
```


Each strategy run writes report files to `reports/latest/...` by default:

- `report.json`
- `report.html`
- `report.txt`

Open `report.html` in a browser for the readable report. Use `report.json` later
for automation, storage, or a .NET UI.

## ETF Momentum Strategy

The active strategy lives in
`src/finance_decisions/strategies/etf_momentum/`.

The strategy rules, the positions file format, and how to act on each report are
documented in
[`src/finance_decisions/strategies/etf_momentum/README.md`](src/finance_decisions/strategies/etf_momentum/README.md).

Edit
`src/finance_decisions/strategies/etf_momentum/positions.json` after each trade
(one entry per purchase, plus `cash`). It is the portfolio source for both the
weekly risk monitor and the monthly plan.

Defaults live in
`src/finance_decisions/strategies/etf_momentum/config/strategy.toml`. The most
important setting to check is your monthly contribution:

```toml
[plan]
monthly_contribution = 400.0
```

Use the config file for normal changes. Use GitHub Actions repository variables
when you want to override values without editing code or committing a config
change.

## Adding Another Strategy

Use this standard for every strategy:

- Put strategy-specific code and runtime files in
  `src/finance_decisions/strategies/<strategy_name>/`.
- Include a strategy `README.md`, `config/strategy.toml`, and any strategy-owned
  state files such as `positions.json`.
- Add module entrypoints inside the strategy package, for example `scanner.py`,
  `risk_monitor.py`, or `runner.py`.
- Keep reusable infrastructure in `src/finance_decisions/` or, if it grows into
  a clear shared layer, `src/finance_decisions/shared/`.
- Do not add root-level `scripts/` wrappers or a separate root `strategies/`
  directory.

Suggested strategy package:

```text
src/finance_decisions/strategies/my_strategy/
├── config/
│   └── strategy.toml
├── README.md
├── positions.json
├── runner.py
└── scanner.py
```

Run it with:

```bash
PYTHONPATH=src python -m finance_decisions.strategies.my_strategy.scanner
```

When a strategy becomes real, add a dedicated GitHub Actions workflow for it or
extend an existing workflow with a matrix.

### Plugging it into the app (reports included)

The .NET backend and the Angular reports know no strategy by name; a new type needs no change there.

1. Write `decide(context, parameters) -> StrategyDecision` and `assess_risk(context, parameters) -> RiskAssessment`
   in the strategy package (see `etf_momentum/decision.py`), a pydantic model of its parameters (the app builds the
   settings form from its JSON Schema) and `engine_version()`. Raise
   `finance_decisions.strategies.errors.ContextError` when the context cannot be used (a missing series, no prices).
2. Register it in `STRATEGY_TYPES` (`api/catalog.py`): code, name, description, parameters, the two functions,
   the series roles it needs and `version=engine_version`.
3. Describe the report's "why" table in `Rationale`:
   - `title` and `instrument_label` — the heading and the first column ("Чому саме ці акції", "Акція");
   - `columns` — `RationaleColumn(id, label, format, hint, compact, true_hint, false_hint)` in display order;
     `format` is `percent`, `signedPercent`, `number`, `money`, `trend` (a bool drawn as an arrow) or `text`;
     `compact=True` keeps the column on a phone; an id cannot be `key`;
   - each `InstrumentScore` carries `values` under those ids, plus the common `rank`, `status`, `reason` (the text
     under "Рішення") and, when the strategy has one, its main score in `momentum` (the backend quotes rank and score
     in the reasons of its buy orders).
   The momentum-specific fields of `InstrumentScore` (`return_1m`, `above_moving_average`, …) are optional; leave them out.
4. Write the reasons and the summary in Ukrainian: the report shows them as they come. The report is a snapshot,
   so text and columns of an old report never change when the strategy does.
5. Regenerate the contract (`python tools/export_contract.py`) and test `decide` through the API like
   `tests/test_api_contracts.py` does.

What the backend still assumes: a decision is target weights plus the share of the contribution to invest, and a
weak market parks the rest. A strategy that needs other order mechanics (stops, hedges) needs the rebalancer extended.

## Deploy To GitHub

Use a private GitHub repository unless all positions and reports are safe to
share publicly. The repository contains strategy state in
`src/finance_decisions/strategies/etf_momentum/positions.json`.

### 1. Create The Repository

1. Open GitHub and create a new repository.
2. Choose **Private**.
3. Do not add a README, `.gitignore`, or license on GitHub if this local project
   already has them.
4. Copy the repository URL, for example:

```text
git@github.com:<your-user>/finance-decisions.git
```

### 2. Push This Project

From the project root:

```bash
git init
git add README.md pyproject.toml requirements.txt requirements-dev.txt src tests .github
git commit -m "Initial finance decisions app"
git branch -M main
git remote add origin git@github.com:<your-user>/finance-decisions.git
git push -u origin main
```

If the repository already exists locally with commits, use only:

```bash
git remote add origin git@github.com:<your-user>/finance-decisions.git
git push -u origin main
```

If `origin` already exists, update it instead:

```bash
git remote set-url origin git@github.com:<your-user>/finance-decisions.git
git push -u origin main
```

### 3. Confirm Actions Are Enabled

1. Open the repository on GitHub.
2. Go to **Actions**.
3. If prompted, enable workflows for the repository.
4. Open the `CI` workflow and confirm tests run on the latest commit.

The first push should run:

```text
.github/workflows/ci.yml
```

### 4. Run The Monthly Scanner Manually

1. Go to **Actions**.
2. Select **Monthly ETF Scanner**.
3. Click **Run workflow**.
4. Wait for the workflow to finish.
5. Open the completed workflow run.
6. Download the `monthly-scanner-report` artifact.
7. Open `report.html`.

The workflow command is:

```bash
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner
```

### 5. Run The Weekly Risk Monitor Manually

1. Go to **Actions**.
2. Select **Weekly Risk Monitor**.
3. Click **Run workflow**.
4. Download the `weekly-risk-monitor-report` artifact after the run finishes.

The workflow command is:

```bash
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.weekly_risk_monitor
```

### 6. Scheduled Runs

The included workflows run automatically:

- Weekly risk monitor: every Sunday at 17:00 UTC.
- Monthly scanner: first Sunday of each month at 18:00 UTC.

Both run after US and European markets have closed, so every price is a full
close. Act on the results on Monday. GitHub scheduled workflows use UTC time.

### 7. Keep Positions Updated

After every real trade, update:

```text
src/finance_decisions/strategies/etf_momentum/positions.json
```

Then commit and push:

```bash
git add src/finance_decisions/strategies/etf_momentum/positions.json
git commit -m "Update ETF momentum positions"
git push
```

The next weekly risk monitor will use the pushed positions file.

## GitHub Actions

The workflows were removed on 2026-10-25: the .NET service now generates the monthly
report and the weekly risk review. Run the CLI commands below locally when needed.

### First GitHub Setup

1. Push this project to a private GitHub repository.
2. Confirm
   `src/finance_decisions/strategies/etf_momentum/positions.json` exists.
3. Open the repository in GitHub.
4. Go to **Actions**.
5. Select `Weekly Risk Monitor` or `Monthly ETF Scanner`.
6. Click **Run workflow** for a manual first run.
7. Open the completed run and download the report artifact.
8. Open `report.html` from the artifact.

### Telegram Setup

Optional Telegram alerts for the weekly monitor use repository secrets:

- `TELEGRAM_TOKEN`
- `TELEGRAM_CHAT_ID`

Set them in GitHub:

1. Go to **Settings**.
2. Open **Secrets and variables**.
3. Open **Actions**.
4. Add repository secrets named `TELEGRAM_TOKEN` and `TELEGRAM_CHAT_ID`.

The monthly scanner can also send Telegram if the same secrets are present.

### Optional GitHub Variables

Strategy defaults live in
`src/finance_decisions/strategies/etf_momentum/config/strategy.toml`. You can
override selected values with GitHub Actions repository variables:

- `ETF_LIST` as comma-separated tickers, for example `SXR8.DE,SXRV.DE` or `SPY,QQQ,GLD`
- `CASH_ETF`, `MONTHLY_CONTRIBUTION`, `WEAK_MARKET_INVEST_PCT`
- Scanner: `TOP_N`, `LOOKBACK_MONTHS`, `SCANNER_MA_PERIOD`, `MAX_VOLATILITY`,
  `MAX_DRAWDOWN`, `MAX_VIX`, `MIN_SHARPE`, `MIN_MOMENTUM`, `HOLD_BUFFER_K`,
  `MAX_PER_GROUP`
- Risk monitor: `STOP_LOSS_PCT`, `DRAWDOWN_WARN`, `DRAWDOWN_EXIT`,
  `MA_EXIT_BAND`, `VIX_WARN`, `VIX_EXIT`, `PORTFOLIO_WARN`, `PORTFOLIO_EXIT`

The workflows load the EU config (`strategy.toml`). To run the US universe, change
`STRATEGY_CONFIG` in the workflow to `strategy_us.toml`.

Set variables in GitHub:

1. Go to **Settings**.
2. Open **Secrets and variables**.
3. Open **Actions**.
4. Choose the **Variables** tab.
5. Add only the variables you want to override.

Example `ETF_LIST` value:

```text
SPY,QQQ,GLD,XLK,TLT
```

## Reports

Every strategy run creates three files:

- `report.html` - the decision report: verdict, numbered order checklist, reasons, and details.
- `report.json` - structured data for future automation or .NET UI.
- `report.txt` - console-friendly summary.

GitHub Actions uploads these files as artifacts. For now, artifacts are the
latest processing history. For long-term history, add a bucket later:

- Cloudflare R2 for simple, cheap S3-compatible storage.
- Azure Blob Storage if the future .NET app will live in Azure.

## Future .NET App

If this project grows into a production application, use
`docs/dotnet-migration.md` as the migration roadmap. It covers the recommended
.NET architecture, storage options, phased migration plan, and when it is worth
moving beyond the current GitHub Actions setup.

## Important

This tool is decision support, not financial advice. Review every signal
manually before trading.
