# Investment Strategy Lab

Personal investment service: accounts split into sub-portfolios, each run by a strategy; the service makes reports
(rebalance plans, risk reviews) the user executes at their broker and marks as done. Deployed as one Home Assistant add-on.

| Folder | What | Own notes |
|---|---|---|
| `backend/` | .NET 10 modular monolith + Aspire AppHost (starts Postgres, the API, the strategy engine, the frontend) | `backend/CLAUDE.md` |
| `frontend/` | Angular 21 + PrimeNG + Tailwind SPA | `frontend/CLAUDE.md` |
| `strategy-engine/` | Python `finance_decisions`: strategies behind a FastAPI HTTP API | `strategy-engine/CLAUDE.md` |
| `deploy/home-assistant/` | The add-on: one container with Postgres + engine + API serving the SPA | `deploy/home-assistant/CLAUDE.md` |
| `docs/architecture/` | `migration-plan.md` (the design, sections referenced as §N and decisions as DN) and `adr/` | — |

## Commands

```bash
# Whole stack (needs Docker for Postgres)
cd backend && dotnet run --project src/Hosts/InvestmentLab.AppHost --launch-profile https

cd backend && dotnet build InvestmentLab.slnx && dotnet test --solution InvestmentLab.slnx   # integration tests need Docker
cd strategy-engine && uv run pytest -q
cd frontend && npx ng build
```

## How the pieces talk

- Strategy types live in the engine; the backend syncs their catalog (`GET /v1/strategies`) and asks them for
  decisions per sub-portfolio. The contract is `strategy-engine/contracts/v1/openapi.json`: a change on one side
  usually needs the other side (backend `Strategies` module, frontend models) in the same change.
- Reports a strategy type declares (code, name, mode, default schedule) drive what the backend schedules and generates
  and what the frontend lists — nothing about a specific report is hard-coded elsewhere.
- Reports are generated in the background (ADR-0005): a `ReportRequest` polls the strategies, then the report is
  published as an integration event; Notifications turns events into Home Assistant notices.

## Conventions

- Everything the user sees is in **Ukrainian** (UI, report texts, notifications, add-on docs). Code, identifiers and
  code comments are in English. A report is «рапорт» in the UI.
- Comments and doc comments say what a thing means for the user or the domain, in full sentences; match the density
  of the surrounding code rather than adding narration.
- Commit messages: one descriptive paragraph of what changed for the user and why (see `git log`), no type prefixes.
- Do not bump `deploy/home-assistant/investlab/config.yaml` `version` by hand: a workflow bumps it on every push to
  `main` that touches `backend/`, `frontend/`, `strategy-engine/` or the add-on. Backend pushes are also auto-formatted
  by `dotnet format` and committed back — pull before pushing again.
