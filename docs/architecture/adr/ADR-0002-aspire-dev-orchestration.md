# ADR-0002: .NET Aspire для локального запуску

- Статус: прийнято (2026-10-25)
- Контекст: [migration-plan.md](../migration-plan.md), §3

## Контекст
Локально потрібні Postgres, API, Python-рушій стратегій і Angular dev server. Запуск має бути «одна кнопка» в Rider.

## Рішення
`InvestmentLab.AppHost` (Aspire 13) описує всі ресурси. Postgres живе в persistent-контейнері з volume
`investmentlab-postgres-data`, тому котирування й журнал переживають перезапуски. Dashboard і DCP приходять NuGet-пакетами
(`AspireUseCliBundle=false`), тож Aspire CLI не обов'язковий. Інтеграційні тести запускають той самий AppHost
через `Aspire.Hosting.Testing` з `InvestmentLab:EphemeralDatabase=true`.

## Наслідки
- Потрібен Docker.
- ServiceDefaults дають OpenTelemetry, health checks і resilience для всіх .NET-сервісів.
