# ADR-0004: Python-сайдкар як рушій стратегій

- Статус: прийнято (2026-10-25)
- Контекст: [migration-plan.md](../migration-plan.md), D4–D6, §12

## Рішення
Наявний код `finance_decisions` переїжджає в `strategy-engine/` без змін логіки й отримує FastAPI-обгортку.
.NET викликає його через порт `IStrategyEngine`. Рушій видає цілі (ваги, invest fraction, ризик-сигнали), а не ордери;
ордери рахує .NET (`SubPortfolioRebalancer`). Ціни передає .NET; рушій сам їх не завантажує, крім `/prices` для MarketData.

## Наслідки
- Legacy-флоу GitHub Actions працює з `strategy-engine/` до кінця паралельного запуску (етап 9).
- Контракт `/v1/...` версіонується, JSON Schema перевіряється з обох боків.
