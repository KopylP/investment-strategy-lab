# ADR-0007: Domain events of aggregates become integration events in the outbox

- Status: accepted (2026-10-31), complements [ADR-0003](ADR-0003-outbox-and-scheduler.md)
- Context: [ADR-0006](ADR-0006-signals-module.md)

## Context
Until now a handler called `repository.Publish(integration event)` itself. So the scenario, not the aggregate, decided what had happened in the domain,
the repository was responsible for publishing, and the contract between modules leaked into the scenario. The base `AggregateRoot` already could collect domain events, but nobody used them.

## Decision
- **The aggregate raises a domain event** (`Raise`) in the method that changes the state; this is a plain in-memory record with no side effects. The event is named in domain language in the past tense and carries `OccurredAt`.
- **A save interceptor** (`DomainEventsInterceptor`, `SaveChangesInterceptor`) collects, before `SaveChanges`, the events of tracked aggregates, including those being deleted, and clears them.
- **A module mapper** (`IDomainEventMapper<TDomainEvent, TIntegrationEvent>`, `AddDomainEventMapper`) turns a domain event into a contract. The module decides itself what to publish: an event without a mapper stays inside the module.
- **One transaction:** outbox rows are added to the same `SaveChanges` as the state change, so both are saved or neither is. The outbox dispatcher does not change: delivery is at least once, and consumers are idempotent.
- **An event without an aggregate:** a fact that changes no aggregate (a nightly quote import, a rejected transaction) is recorded through `ModuleDbContext.Record` and then follows the same path.
- There is no broker and no MediatR. There are also no domain event handlers inside a module yet; when they are needed, they will be a separate step before the mappers.

## Consequences
- All modules are migrated: repositories no longer publish, and `ModuleDbContext.Publish` is available only to the outbox mechanism.
- The mapper does not query the database: everything it needs is carried by the event itself or by the aggregate that raised it, in its state at the time of saving. The text for the user is produced by the mapper, not by the domain.
- A change to a contract does not touch the aggregates; whether an aggregate raised an event can be checked without a database.

## Sources
- [Domain events: design and implementation](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation), deferred dispatch on `SaveChanges` and the difference between domain and integration events.
- [Subscribing to events](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/subscribe-events), the table of events in a local transaction and idempotency.
- [Modular monolith with DDD](https://github.com/kgrzybek/modular-monolith-with-ddd), an outbox per module and at-least-once delivery.
