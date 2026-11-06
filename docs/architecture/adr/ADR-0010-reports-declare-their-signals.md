# ADR-0010: A strategy declares which built-in signals trigger its report

- Status: accepted (2026-11-01)

## Context
The link "signal → report" (`SignalReportRule`, [ADR-0006](ADR-0006-signals-module.md)) was created only by hand, so after an account appeared, the built-in signals triggered no report, although the strategy knows that its risk check is needed exactly then.

## Decision
- `ReportDefinition.signalKinds` in the engine contract names the **types** of built-in signals, not specific signals: the signals themselves are created separately for each owner and account. Only what is needed is declared: at present the risk check; rebalancing on a signal stays a manual choice, because it proposes buys and sells.
- `Signals` exposes built-in signals through `IBuiltInSignalQueries`, and still knows nothing about reports. The signal type is derived only by `BuiltInSignalKind`; an unknown key gives an error.
- `Rebalancing` reconciles the rules with a periodic job (`EnsureDeclaredSignalRules`): the job is idempotent and equally covers a new strategy, a new account and a new signal, so separate reactions to each event are not needed.
- The user's choice outweighs the declaration: any deleted rule, declared or manual, becomes dismissed (`DismissedAt`) and does not come back by itself. If it is added again, it becomes manual, and the job never touches manual rules.
- An account with automatic generation switched off (`GenerateOnSchedule = false`) gets no declared rules.

## Consequences
- A new strategy gets a reaction to signals with one line in its description.
- A new type of built-in signal needs a value in the engine's `SignalKind` and in `BuiltInSignalKind`.
- A new signal waits for the job's next run (every minute), and a new declaration by a strategy also waits for the catalog update (up to an hour). Until then, a firing does not trigger a report.
- Records of dismissed rules remain in the table until the signal itself is deleted.
