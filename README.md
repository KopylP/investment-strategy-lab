# Finance Decisions

Small Python app for strategy-level finance decision support. The active ETF
momentum strategy provides:

- a monthly ETF momentum scanner
- a weekly portfolio risk monitor

## Project Layout

```text
.
├── .github/workflows/
│   ├── ci.yml
│   ├── monthly_scanner.yml
│   └── weekly_risk_monitor.yml
├── src/finance_decisions/
│   ├── config.py
│   ├── market_data.py
│   ├── models.py
│   ├── notifications.py
│   ├── reports.py
│   ├── risk.py
│   ├── scanner.py
│   └── strategies/
│       └── etf_momentum/
│           ├── config/
│           │   └── strategy.toml
│           ├── README.md
│           ├── monthly_scanner.py
│           ├── positions.json
│           ├── runner.py
│           └── weekly_risk_monitor.py
├── tests/
├── requirements.txt
└── requirements-dev.txt
```

All strategy-specific code, docs, config, and runtime position state live under
`src/finance_decisions/strategies/<strategy_name>/`. Shared reusable
infrastructure, such as market data, reports, notifications, config loading,
risk primitives, scanner logic, and position models, stays in
`src/finance_decisions/`.

There are no root-level strategy wrappers. Run strategies through module
entrypoints so local commands, tests, and GitHub Actions all use the same code.

## Local Setup

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements-dev.txt
```

## Run Locally

```bash
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.monthly_scanner
PYTHONPATH=src python -m finance_decisions.strategies.etf_momentum.weekly_risk_monitor
pytest -q
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

Edit
`src/finance_decisions/strategies/etf_momentum/positions.json` after each trade.
This file is the current portfolio source for the weekly risk monitor and the
comparison baseline for the monthly scanner.

Defaults live in
`src/finance_decisions/strategies/etf_momentum/config/strategy.toml`.

```toml
[risk]
stop_loss_pct = -0.08
drawdown_warn = -0.10
drawdown_exit = -0.18

[scanner]
top_n = 3
lookback_months = 6
max_volatility = 0.35
max_drawdown = -0.20
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

- Monthly scanner: first Monday of each month at 09:00 UTC.
- Weekly risk monitor: every Monday at 08:00 UTC.

GitHub scheduled workflows use UTC time. For Poland, remember that local time
changes with daylight saving time.

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

Workflows are in `.github/workflows`:

- `CI` runs tests on push, pull request, and manual dispatch.
- `Weekly Risk Monitor` runs every Monday at 08:00 UTC.
- `Monthly ETF Scanner` runs on the first Monday of each month at 09:00 UTC.

Both strategy workflows can also be started manually with the **Run workflow**
button. They upload the latest JSON, HTML, and text report as GitHub Actions
artifacts with 90-day retention.

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

- `ETF_LIST` as comma-separated tickers, for example `SPY,QQQ,GLD`
- `TOP_N`
- `LOOKBACK_MONTHS`
- `STOP_LOSS_PCT`
- `DRAWDOWN_WARN`
- `DRAWDOWN_EXIT`
- `VIX_WARN`
- `VIX_EXIT`
- `PORTFOLIO_WARN`
- `PORTFOLIO_EXIT`
- `MAX_VOLATILITY`
- `MAX_DRAWDOWN`

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

- `report.html` - human-readable decision report with summary cards, alerts, actions, and tables.
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
