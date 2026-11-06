# ADR-0006: A signals module that other modules react to

- Status: accepted (2026-10-31)
- Context: [ADR-0003](ADR-0003-outbox-and-scheduler.md), [ADR-0005](ADR-0005-async-report-generation.md), D14

## Context
The user wants to know about important changes in the market and in their portfolio (an account drawdown, a position falling, a high VIX),
and the system should react flexibly: send notifications, build reports and, later, influence strategies.
Today strategies see the market only when a scheduled report is due.

## Decision
- **A new module `Signals`** only announces that a condition fired. The `Signal` aggregate raises domain events, and mappers turn them into
  `SignalTriggeredV1`, `SignalClearedV1`, `SignalDeletedV1` in the outbox ([ADR-0007](ADR-0007-domain-events-to-outbox.md)). The module knows nothing about reports and notifications; the reactions live in the modules that carry them out.
- **A signal is data, not code:** a subject (an instrument, each position of an account, a sub-portfolio, an account), a metric (drawdown from peak,
  fall in a day, loss from cost, level), a threshold and a rearm threshold. Built-in signals have the same model as the user's signals.
- **Firing only when the threshold is crossed.** The event happens once; the signal can fire again after returning past the rearm threshold
  (by default 5/6 of the threshold: −30% → −25%). The state is the open occurrence (`SignalOccurrence`) keyed by "signal + subject", and it is also the history.
- **One event per check:** several positions that fell together give one event with a list, so one notification and one report request.
- **Checking:** a job picks the signals that are due once a minute. By default "after the data is updated" (`Cadence` off; polling every 10 min,
  the result on the same data does not change), or by the signal's own `Cadence`. A crossing on the same numbers is impossible, so repeated checks do nothing.
- **Bad data:** quotes with a `Spike` are ignored, and a subject with a stale quote is not assessed.
- **Calculation:** a position's drawdown by `AdjustedClose` in the trading currency, the peak no earlier than the first purchase; the loss in the account currency;
  the drawdown of an account and a sub-portfolio by the TWR index, so a withdrawal does not look like a fall.
- **The "report" reaction** is a launch policy next to the schedule: `SignalReportRule` in `Rebalancing.Planning`. The request goes through the existing `RequestReportHandler`
  with `ReportTrigger.Signal`. An open monthly report is not overwritten (D14), the contribution in such a report is zero, and the reason is visible in its warnings.
  Idempotency: a unique key "occurrence + account + report code". An impossible launch stays in the rule as a `Problem`.
- **The "notification" reaction** in `Notifications`: by default we notify, and the user mutes individual signals (`MutedSignalIds`). Clearing a signal removes the notification.
- **Strategies unchanged.** A report from a signal is the same report, only earlier; strategies do not know about the signal.
- `ReportCadence` became a general `Cadence` in `SharedKernel`, because reports and signals share its logic.

## Why not a separate reactions module
For a simple "one signal → one reaction" it would only store links and forward calls, and the contracts would have to grow commands.
It is worth introducing when composite rules appear ("A and B, then a report and a strategy pause").

## Consequences
- A new reaction does not change `Signals`: subscribing to the event is enough. Strategies will later get a query of active signals.
- The data is daily only, so a signal about a fall arrives after the market closes. Intraday quotes are a separate stage: separate storage and a request in the engine, not `DailyQuote`.
- All signals, market ones included, belong to a user: this way everyone has their own thresholds, reactions and history, and events do not mix other people's data.
- Built-in signals are created by themselves for each account; they can be changed or turned off, but not deleted, otherwise they would appear again.
