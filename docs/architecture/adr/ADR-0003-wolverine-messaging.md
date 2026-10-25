# ADR-0003: Wolverine для повідомлень, outbox і задач за розкладом

- Статус: прийнято (2026-10-25)
- Контекст: [migration-plan.md](../migration-plan.md), §3, §9, Q4

## Рішення
Wolverine (MIT): in-process bus, durable outbox/inbox на Postgres, scheduled/recurring messages.
Integration events між модулями йдуть через outbox; транспорт пізніше міняється на RabbitMQ без зміни handler-ів.
Пакети додаються разом із першим модулем, якому потрібні повідомлення (MarketData, нічне завантаження котирувань).
