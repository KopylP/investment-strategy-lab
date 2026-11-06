# ADR-0003: Our own outbox and scheduler instead of Wolverine

- Status: accepted (2026-10-25), **replaces** the earlier version ("Wolverine for messaging"); how events are written to the outbox is refined by [ADR-0007](ADR-0007-domain-events-to-outbox.md)

## Context
Modules need: reliable integration events between them (a state change and its event in one transaction, retries when a consumer fails),
scheduled jobs (quotes, reports, risk) and the option to move a module into a service one day.
Wolverine covers this, but for one process and one user it is too heavy: code generation at startup,
Roslyn/MSBuild among the dependencies, implicitly applied transactions — hard to debug in Rider.

## Decision
- An `outbox_messages` table in each module's schema; `ModuleDbContext.Publish(event)` adds a record, `SaveChanges` stores it together with the change.
- `OutboxDispatcher` (`BackgroundService`) reads unprocessed records and calls the consumers' `IIntegrationEventHandler<T>` in-process; on failure it retries with a growing delay. Consumers are idempotent (the key is the message id or `SourceReference`).
- Scheduler: `IScheduledJob` with a schedule in a time zone, hosted by a `BackgroundService`.
- Commands and queries are direct calls of a handler from DI; there is no bus.

## Consequences
- The whole mechanism is a few hundred lines in `BuildingBlocks`, visible end to end.
- When a module is extracted into a service, the outbox dispatcher publishes to a broker (RabbitMQ) instead of calling in-process; handlers do not change.
- One application instance: no distributed locking of jobs and the outbox (add `FOR UPDATE SKIP LOCKED` if there are ever more instances).
