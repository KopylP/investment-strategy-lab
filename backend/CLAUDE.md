# Backend (.NET 10 modular monolith)

Solution `InvestmentLab.slnx`. Design: `../docs/architecture/migration-plan.md` and `../docs/architecture/adr/`.

## Commands (from `backend/`)

```bash
dotnet build InvestmentLab.slnx
dotnet test --project tests/Modules/InvestmentLab.Rebalancing.Tests     # one project; fast, no Docker
dotnet test --project tests/InvestmentLab.ArchitectureTests             # dependency rules; run after moving code
dotnet test --solution InvestmentLab.slnx                               # all; IntegrationTests need Docker (Aspire)
dotnet format InvestmentLab.slnx --verify-no-changes --severity info    # what CI enforces
dotnet tool restore && dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/InvestmentLab.<Module> --output-dir Shared/Infrastructure/Migrations
```

Tests are xunit v3 on Microsoft.Testing.Platform with Shouldly (`dotnet test --project …`, not a positional path).

## Layout

- `src/Hosts/` — `InvestmentLab.Api` (composes the modules), `InvestmentLab.AppHost` (Aspire), `ServiceDefaults`.
- `src/BuildingBlocks/` — `SharedKernel` (Result/Error, value objects, `YearMonth`…; no frameworks) and `BuildingBlocks`
  (messaging/outbox, persistence, `IScheduledJob` scheduling, `IModule`).
- `src/Modules/<Module>/` — `InvestmentLab.<Module>` and `InvestmentLab.<Module>.Contracts`. Modules: Identity,
  Portfolio, MarketData, Strategies, Rebalancing, Performance, Taxation, Signals, Notifications.
- Inside a module: feature folders (`Planning/`, `Reports/`…) each with `Domain/`, `Application/`, `Infrastructure/`,
  `Api/`; module-wide pieces in `Shared/`; registration in `<Module>Module.cs`.

## Rules (enforced by `InvestmentLab.ArchitectureTests`)

- A module uses another only through its `.Contracts` project: integration events (`…V1` records) and query
  interfaces implemented inside the owning module. Contracts hold primitives only and reference nothing of ours.
- `Domain` depends only on `SharedKernel`; no EF Core / ASP.NET there.
- Each module has its own `DbContext` and schema; integration events go out through the outbox in the same save
  (ADR-0007): the aggregate raises a domain event, a mapper registered by the module turns it into an integration event
  (`AddDomainEventMapper`). Repositories do not publish.

## Conventions

- Handlers return `Result<T>` with `Error.Validation/NotFound(...)` (code like `report_schedule.day`), not exceptions.
- Time comes from the injected `TimeProvider`; report schedules are local to `ReportSchedules.TimeZone` (Europe/Warsaw).
- Integration events are versioned: add new fields as trailing optional parameters (`= null`) and document that old
  stored events lack them; never change or remove existing ones.
- Formatting and analyzers follow `.editorconfig`; EF migrations count as generated code. Primary constructors,
  collection expressions, file-scoped namespaces.
- Text the user reads (notifications, report warnings, error messages shown in the UI) is Ukrainian.
