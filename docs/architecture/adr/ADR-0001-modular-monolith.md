# ADR-0001: A modular monolith on .NET

- Status: accepted (2026-10-25)

## Context
The current tool is Python scripts in GitHub Actions. A web service is needed with several bounded contexts
(MarketData, Strategies, Portfolio, Rebalancing, Taxation, Performance, Identity). One developer and one user to start with.

## Decision
One ASP.NET Core process (`InvestmentLab.Api`) in which each bounded context is a separate module:
a project `InvestmentLab.<Module>` + `InvestmentLab.<Module>.Contracts`. Modules see each other only through `Contracts`;
each has its own Postgres schema and its own `DbContext`. Inside a module the feature comes first, then the layers `Domain/Application/Infrastructure/Api`.

## Consequences
- Dependency rules are checked by `InvestmentLab.ArchitectureTests` (both at the type level and at the level of references between assemblies).
- Any module can be extracted into a service: the contracts are already isolated, the data lives in its own schema.
- The price: more projects and ceremony than a "simple" application.
