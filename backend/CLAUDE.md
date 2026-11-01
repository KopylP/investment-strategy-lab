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

### What each layer is responsible for

**Business logic lives in `Domain` and is covered by unit tests.** Ask of any decision: "would a domain expert recognise
this rule?" If yes, it belongs in `Domain`, whatever the shape of the code that needs it.

| Layer | Holds | Must not hold | Tested by |
|---|---|---|---|
| `Domain` | Every business rule and calculation: aggregates, value objects, domain services and pure calculators (`TaxCalculator`, `SalesTaxImpactCalculator`, `BrokerTickerMatcher`), domain events, errors, repository and policy interfaces | EF Core, ASP.NET, HTTP, the clock, reading or saving data, other modules' contracts | **Unit tests, required**: each rule, its edge cases and each way it can fail, without Docker or mocks of infrastructure |
| `Application` | The order of steps of one use case: load, call the domain, save, return. Mapping to its own DTOs. A plain read of another module's `.Contracts` query (see Data from other modules) | Business rules, calculations, thresholds, `if`s that decide a business outcome | Only when it has real branching of its own; most is covered through the domain and the integration scenario |
| `Infrastructure` | How the outside world is reached: EF configurations, repositories, HTTP adapters, parsers of external formats, adapters over other modules' contracts | Business decisions; an adapter only translates | Adapter tests where the format is tricky; integration tests otherwise |
| `Api` | Routes, binding, calling one handler, `ToHttpResult()` | Anything else, including validation rules beyond binding | Integration scenario |
| `Contracts` | What other modules may see: integration events and query interfaces, primitives only | Rules or logic beyond trivial derived values; references to anything of ours | — |

- **When a handler grows a rule, move the rule down.** Extract it to the aggregate or to a pure domain function that takes
  plain inputs and returns a result, and test that function. The handler gathers the inputs and passes them in.
- **Prefer pure functions for calculations:** no I/O and no injected services, so a test states inputs and the expected
  numbers. `TaxEstimator` (Application) gathers data; `SalesTaxImpactCalculator` (Domain) decides.
- **A new or changed business rule comes with its unit tests in the same change,** in
  `tests/Modules/InvestmentLab.<Module>.Tests`. Plumbing (DTOs, endpoints, configuration, mapping) needs no test of its
  own; one integration scenario per feature proves the wiring.
- **Test names state the rule** in plain words, so the test list reads as the rules of the domain.
- Older handlers that still hold rules are moved down when they are next changed.

### Inside a module

Every file has one place. Nothing lies loose in `Application/` or in the module's root except `<Module>Module.cs`.

```
Modules/<Module>/
├── InvestmentLab.<Module>.Contracts/     integration events (…V1) and query interfaces: primitives only
└── InvestmentLab.<Module>/
    ├── <Module>Module.cs                 registration
    ├── <Feature>/                        Accounts, Planning, Monitoring…
    │   ├── Api/                          endpoints of the feature
    │   ├── Application/
    │   │   ├── <ActionName>/             one use case: its handler or job, with the command and DTOs only it uses
    │   │   ├── Integration/              reactions to other modules' events, one handler per event
    │   │   └── Shared/                   what several actions of the feature use (DTOs, inputs, helpers)
    │   ├── Domain/                       aggregates, value objects, domain events, errors, repository interfaces
    │   └── Infrastructure/               EF configurations, repositories, outside services
    └── Shared/Infrastructure/            the module's DbContext and Migrations/
```

- `Application/` holds only folders, never a file directly: `<ActionName>/`, `Integration/` and `Shared/`.
- Repository interfaces (`IAccountRepository`) go in `Domain/`. Request records and mappings that several actions use
  (`ContributionPolicyChoice`, `AccountMapping`) go in `Application/Shared/`; used by one action, in that action's folder.
- `<ActionName>` is a verb phrase and the namespace follows the folder: `Accounts/Application/OpenAccount/OpenAccountHandler.cs`.
- A background job is an action too: `Monitoring/Application/CheckDueSignals/SignalsJob.cs`.
- A type used by one action stays in that action's folder. It moves to `Application/Shared/` when a second action needs it,
  and to `Domain/` when it is a concept of the domain rather than of the use cases.
- A new action, or a split or move of existing code, leaves the tree in this shape in the same change.

### Inside an action

One file `<ActionName>Handler.cs` holds the command record, the DTOs only this action uses, and the handler with one
public method `Handle`. It reads as steps; rules live in the aggregate.

```csharp
public sealed record RetireSubPortfolioCommand(Guid AccountId, Guid SubPortfolioId);

public sealed class RetireSubPortfolioHandler(IAccountRepository accounts, TimeProvider clock)
{
    public async Task<Result> Handle(RetireSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(command.AccountId, cancellationToken);
        if (account is null)
            return AccountErrors.NotFound(command.AccountId);

        var retired = account.RetireSubPortfolio(command.SubPortfolioId, clock.GetUtcNow());
        if (retired.IsFailure)
            return retired;

        await accounts.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

- **A command** changes state: load the aggregate, call its method, save. The order is guards, gathering data, the domain
  call, saving, returning, with a blank line between them. The handler has no business rules and publishes nothing: the
  aggregate raises a domain event, and a mapper turns it into an integration event in the same save.
- **A query** only reads: load, map to a DTO, return. It changes nothing and raises nothing. Name it `Get…` or `List…`.
  Data that is derived and kept on demand (tax assessments, valuation snapshots) is made fresh by its own `Refresh…`
  handler, and the endpoint named `RefreshAndGet…` calls that handler and then the query.
- **Failures** are returned as `Result` with an error from `<Feature>Errors` in `Domain/`, not thrown. Parsing input into
  value objects is a private method or lives on the input DTO.
- **An endpoint** is a named private method that calls one handler and returns `ToHttpResult()`; the route table at the
  top of the file only maps routes to those methods.
- **An integration event handler** reacts to one event, is idempotent because delivery is at-least-once, and is named
  for what it does (`RequestReportsOnSignal`).
- **A job** only decides what is due and calls a handler per item, each in its own scope, so one failure does not stop
  the others.

### Data from other modules

**A DTO of another module never leaves the class that reads it.** It may be a local variable, a private member or the
argument of a private method of the class that calls the contract, and nothing else: it is not in a public or internal
signature, not in a property of a record or command, and not passed to another class. `ForeignContractIsolationTests`
checks signatures, fields and base types; it cannot see a DTO passed as `object` or a generic `T`, so never hide one that way.

**When what is used.** Ask the questions in this order and take the first that fits:

| The situation | Use | Where |
|---|---|---|
| A use case only checks that something exists, or copies a few values out of it (owner of an account, a name) | The `.Contracts` query injected into the handler; the DTO stays inside the handler | The handler in `Application/<ActionName>/` |
| The data becomes a business concept here, domain logic works on it, several actions need it, or it is combined from several contracts | A port returning domain types, with an adapter | Port in `Domain/`, adapter in `Infrastructure/` |
| Another module's event arrives or is sent | The event record stays in `Application/Integration/`: its handlers, mappers and their helpers. They pass on primitives or domain types. A `…V1` that comes inside a query result (journal entries) follows the DTO rule above | `Application/Integration/` only |
| A use case sends a request to another module or receives its answer as an argument flowing through several classes | A port that takes and returns domain types; the adapter builds the request and reads the answer | Port in `Domain/`, adapter in `Infrastructure/` |

**Never write a class that gathers other modules' data for handlers** (`PlanningInputs`, `TaxInputs` and the like, whatever
the name). Such a class grows into a grab bag: every handler depends on everything it holds and foreign DTOs spread through
the use cases. A record that carries the fields of a request (`NewSubPortfolio`) is a different thing and stays.

**How a port and its adapter look:**

- **The domain states what it needs:** a port in `Domain/` (`IPlanningAccounts`) that returns a domain interface
  (`IPlanningAccount`) or a domain record, holding only the members this module uses, named in this module's language.
- **The adapter lives in `Infrastructure/`** and implements the port over the other module's `.Contracts` query. It wraps
  each DTO in a private nested class that implements the domain interface and only forwards: `=> _dto.Property`. A domain
  record fits instead when the result is an answer copied once and kept (`StrategyDecision`) or a value in this module's terms
  built from several sources (`DeclaredReport`).
- **Rules stay in the domain.** The wrapper forwards and at most converts a primitive to a value object or a flag; a decision
  on top of the data (rounding, conversion, grouping, signs of money) is a method or a pure function in `Domain`, with its unit
  tests, and the adapter calls it.
- **Ports are small and named for what they give.** A second need that is not cohesive with a port is a second port.

```csharp
// Planning/Domain/IPlanningAccount.cs
public interface IPlanningAccount
{
    Guid Id { get; }
    string BaseCurrency { get; }
}

// Planning/Domain/IPlanningAccounts.cs
public interface IPlanningAccounts
{
    Task<IPlanningAccount?> FindAsync(Guid accountId, CancellationToken cancellationToken);
}

// Planning/Infrastructure/PlanningAccounts.cs
internal sealed class PlanningAccounts(IAccountSetupQueries accounts) : IPlanningAccounts
{
    public async Task<IPlanningAccount?> FindAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var setup = await accounts.GetAccountSetup(accountId, cancellationToken);

        return setup is null ? null : new Account(setup);
    }

    private sealed class Account(AccountSetupDto dto) : IPlanningAccount
    {
        private readonly AccountSetupDto _dto = dto;

        public Guid Id => _dto.Id;
        public string BaseCurrency => _dto.BaseCurrency;
    }
}
```

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
- Text the user reads never appears in code: warnings, reasons and notes are `Message.Of(code, parameters)`, errors carry a
  code and parameters, and the words are in `../locales/uk.json` (see Language in `../CLAUDE.md`).
