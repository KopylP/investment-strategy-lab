# ADR-0031: Signals state the return history they need, Performance provides it

- Status: accepted (2026-11-13)

## Context

Modules use each other through `.Contracts` projects, and until now a query interface in `.Contracts` was always
implemented by the module that owns the project. Three such uses formed a cycle:

- **Rebalancing → Signals.** Rebalancing keeps the signal rules a report declares (ADR-0010), requests reports when a
  signal triggers and removes rules of a deleted signal: `ISignalDirectory`, `SignalTriggeredV1`, `SignalDeletedV1`.
- **Signals → Performance.** A portfolio signal (a day's fall, a drawdown from the peak) measures the time-weighted
  return index of an account or a sub-portfolio: one call, `IValuationHistory.GetTwrHistory`, in `PortfolioSubjectReader`.
- **Performance → Rebalancing.** Performance keeps a `PlannedReport` from `ReportGeneratedV1` and `ReportSupersededV1`.
  It is not a copy kept only for valuation: the planned actions drive the shadow portfolio (`ShadowPortfolioSimulator`),
  whose valuation is the stored `FullPlan` scenario drawn as the plan line next to the actual one, the reports are the
  markers on the account history, and a new or superseded report marks the account's snapshots for a rebuild.

A cycle means none of the three can be changed or reasoned about without the other two, and nothing stopped it from
growing. Two cuts were considered:

- **Rebalancing owns the plan.** Performance would ask Rebalancing for the planned actions instead of storing them. This
  does not break the cycle: Performance still needs the actions to value the shadow portfolio, so it would still depend on
  Rebalancing, only through a query instead of events. Breaking the cycle this way means moving the full-plan valuation
  out of Performance and its stored rows (`planned_reports` and the `FullPlan` snapshots) between schemas: a large change
  with a data move, for no gain to the user.
- **Signals states what it needs.** Signals depends on Performance for one call. The interface moves into
  `Signals.Contracts` as `IPortfolioReturnHistory` with its `TwrPointDto`, and Performance implements it with the
  adapter it already had. Signals no longer depends on Performance; Performance depends on Signals' contract, next to its
  dependencies on Portfolio and Rebalancing. No stored data, event or behaviour changes.

## Decision

- **Signals owns `IPortfolioReturnHistory`** in `InvestmentLab.Signals.Contracts`, and Performance implements it in
  `Valuation/Infrastructure/PortfolioReturnHistory.cs` and registers it in `PerformanceModule`.
- **A module may state in its own `.Contracts` what it needs from another module** (a required contract), when the
  ordinary direction would close a cycle between modules. The providing module implements it in its `Infrastructure/`,
  registers it in its module file and exposes the contract's DTOs only in the members of that contract
  (`ForeignContractIsolationTests` allows exactly that). Otherwise the rule stays: a query interface is implemented by
  the module that owns the contract.
- `InvestmentLab.Performance.Contracts` stays, empty, like the contracts of Notifications and StrategyBuilder.

## Consequences

- The cycle Rebalancing → Signals → Performance → Rebalancing is gone.
- A reader finds a required contract in the module that uses it, and its implementation by searching for the interface;
  `backend/CLAUDE.md` names the rule so that it is used only to break a cycle.
- If Performance ever needs to stop depending on Rebalancing, moving the plan to Rebalancing is still possible, but it
  means moving the full-plan valuation with it.
