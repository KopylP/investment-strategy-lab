# Investment Strategy Lab

Monorepo for the investment strategy service. The migration plan lives in
[`docs/architecture/migration-plan.md`](docs/architecture/migration-plan.md).

| Folder | What |
|---|---|
| [`strategy-engine/`](strategy-engine/README.md) | Python strategy code (`finance_decisions`): ETF momentum scanner, risk monitor, backtest. The legacy GitHub Actions flow still runs from here. |
| `backend/` | .NET modular monolith (coming next). |
| `frontend/` | Angular app (later stage). |
