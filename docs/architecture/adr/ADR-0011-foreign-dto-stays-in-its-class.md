# ADR-0011: A foreign module's DTO does not leave the class that reads it

- Status: accepted (2026-11-01)

## Context
`PlanningInputs` and `TaxInputs` gathered data of other modules for the handlers. They had grown into aggregate classes with seven dependencies, so every handler depended on everything they held. The DTOs of the Portfolio, MarketData and Strategies modules had spread into records, commands and the planning domain, and a change to a contract touched many scenarios.

## Decision
- A foreign module's DTO may be a local variable, a private member or an argument of a private method of the class that calls the contract. It does not appear in public or internal signatures or properties. `ForeignContractIsolationTests` checks this.
- If a scenario only checks existence or copies a few values, the handler calls the `.Contracts` query itself.
- If the data becomes a business concept of this context, the domain declares a port and its own types, and an adapter in `Infrastructure` implements them over the contract. The DTO is wrapped in a private nested class that only passes properties through (`=> _dto.Property`).
- The `…V1` events stay in `Application/Integration`: they are already the public language between modules, and their handlers and mappers translate it. An event that arrives inside a query response (journal entries) is subject to the DTO rule.
- Classes that gather other modules' data are forbidden, including under names like `…Inputs`. Records that carry request fields (`NewSubPortfolio`) remain.

## Consequences
- A change to a contract touches one adapter or one handler.
- Rules that used to hide in the aggregate classes moved to the domain and received unit tests (`PlannedSubPortfolios`, `PriceWarnings`, `PriceConversion`, `AccountCash`, `AccountReport.AllOf`, `StrategyAsk`, `StrategyTargets.Of`, `JournalMovements`, `LedgerEntries`, `AssessmentHash`).
- The `StrategyRun.Answer` record stays the engine's JSON answer, so saved requests are read as before; only the `StrategyRuns` adapter reads it.
- `AccountReports` reads the account itself, so where a handler also needs the account, the query is made twice. The data lies in the same process, and simplicity matters more.
- Saved strategy answers are now all read at once, not only for the sub-portfolios still in the plan. An answer without required fields gives a request-processing error even when its sub-portfolio has already left. The engine does not send such an answer, and corrupted data should surface, not hide.
