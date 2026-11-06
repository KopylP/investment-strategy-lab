# ADR-0002: .NET Aspire for running locally

- Status: accepted (2026-10-25)

## Context
Locally we need Postgres, the API, the Python strategy engine and the Angular dev server. Starting it must be "one button" in Rider.

## Decision
`InvestmentLab.AppHost` (Aspire 13) describes all resources. Postgres lives in a persistent container with the volume
`investmentlab-postgres-data`, so quotes and the ledger survive restarts. The dashboard and DCP come as NuGet packages
(`AspireUseCliBundle=false`), so the Aspire CLI is optional. Integration tests start the same AppHost
through `Aspire.Hosting.Testing` with `InvestmentLab:EphemeralDatabase=true`.

## Consequences
- Docker is required.
- ServiceDefaults give OpenTelemetry, health checks and resilience to every .NET service.
