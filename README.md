# Investment Strategy Lab

Монорепо сервісу інвестиційних стратегій. Архітектура й етапи міграції —
[`docs/architecture/migration-plan.md`](docs/architecture/migration-plan.md), рішення — [`docs/architecture/adr/`](docs/architecture/adr/).

| Папка | Що |
|---|---|
| [`backend/`](backend/) | .NET 10 модулярний моноліт (`InvestmentLab.slnx`) + Aspire AppHost. |
| [`strategy-engine/`](strategy-engine/README.md) | Python `finance_decisions`: сканер ETF momentum, ризик-монітор, бектест. |
| `frontend/` | Angular (етап 6). |

## Що потрібно

- .NET SDK 10.0.100+ (`backend/global.json`)
- Docker (Desktop або інший) — Aspire запускає в ньому Postgres
- `uv` і Python 3.12 — для `strategy-engine/`
- Rider 2025.3+ (вбудована підтримка Aspire) або `dotnet` CLI
- Довірений dev-сертифікат: `dotnet dev-certs https --trust` (один раз)

## Запуск у Rider

1. Відкрийте `backend/InvestmentLab.slnx`.
2. Запустіть Docker.
3. Виберіть конфігурацію **InvestmentLab.AppHost: https** (Rider створює її з `launchSettings.json`) і натисніть Run або Debug.
4. Відкриється Aspire dashboard (`https://localhost:17258`). Звідти — посилання на API та **Scalar (API docs)**.

Debug з AppHost підключає дебагер і до `InvestmentLab.Api`: брейкпоінти в модулях працюють одразу.

Postgres живе в persistent-контейнері з volume `investmentlab-postgres-data`: дані переживають перезапуски AppHost.
Пароль Aspire генерує один раз і зберігає в user secrets AppHost. Щоб почати з чистої бази:
`docker rm -f $(docker ps -aq --filter name=postgres) && docker volume rm investmentlab-postgres-data`.

## Запуск із терміналу

```bash
cd backend
dotnet run --project src/Hosts/InvestmentLab.AppHost --launch-profile https
```

## Тести

```bash
cd backend
dotnet test --solution InvestmentLab.slnx      # unit + architecture + integration (потрібен Docker)

cd ../strategy-engine
uv run pytest -q
```

Тести .NET працюють на Microsoft.Testing.Platform (xunit v3) — Rider запускає їх зі звичайного Unit Tests вікна.
