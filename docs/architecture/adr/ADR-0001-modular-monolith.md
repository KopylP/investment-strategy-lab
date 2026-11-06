# ADR-0001: Модулярний моноліт на .NET

- Статус: прийнято (2026-10-25)

## Контекст
Поточний інструмент — Python-скрипти в GitHub Actions. Потрібен веб-сервіс із кількома bounded contexts
(MarketData, Strategies, Portfolio, Rebalancing, Taxation, Performance, Identity). Один розробник, один користувач на старті.

## Рішення
Один процес ASP.NET Core (`InvestmentLab.Api`), у якому кожен bounded context — окремий модуль:
проєкт `InvestmentLab.<Module>` + `InvestmentLab.<Module>.Contracts`. Модулі бачать один одного тільки через `Contracts`;
кожен має свою схему Postgres і свій `DbContext`. Усередині модуля — спершу фіча, потім шари `Domain/Application/Infrastructure/Api`.

## Наслідки
- Правила залежностей перевіряють `InvestmentLab.ArchitectureTests` (і на рівні типів, і на рівні посилань між збірками).
- Будь-який модуль можна виділити в сервіс: контракти вже ізольовані, дані — у своїй схемі.
- Ціна: більше проєктів і церемонії, ніж у «простому» застосунку.
