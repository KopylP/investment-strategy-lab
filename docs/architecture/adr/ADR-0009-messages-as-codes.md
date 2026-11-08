# ADR-0009: The backend and the engine pass message codes, and the wording lives in catalogs

- Status: accepted (2026-11-01)

## Context
Warnings, reasons, notes and errors were composed in Ukrainian directly in the backend and engine code. This tied the logic to a language, and the texts could not be seen together or translated.

## Decision
- **A token in a string:** `@code {json-parameters}`. This is an ordinary string, so the columns, the JSON in the database and the contracts did not change, and old records without a token are shown as they are.
- **Catalogs:** `locales/uk.json` for the service and `locales/uk.json` of each strategy in the engine. A strategy's catalog arrives in its description (`GET /v1/strategies`); the backend stores it in `strategy_types.messages` and serves it through `GET /api/strategies/catalog/messages`.
- **The frontend translates everything in one place:** an interceptor walks every JSON API response, so the screens know nothing about codes. A strategy's catalog is loaded when a response contains an unknown code.
- **The backend renders only what does not reach the frontend:** Home Assistant notifications and PDFs. For this, the same catalog is embedded in the `BuildingBlocks` assembly, and `MessageLocalizer` repeats the template format. The engine's command-line reports are rendered by `messages.render`.
- **Templates:** `{x}`, `{x:percent}`, `{x:signedPercent}`, `{x:money:currencyParam}`, `{x:amount}`, `{x:plural:one|few|many}`, `{x:join:separator}`. A parameter may itself be a token, so messages are not glued together by interpolation.

## Consequences
- There are three implementations of the templates: TypeScript, C# and Python. The C# formatting is checked by golden tests: they compare the rebalancer's texts with the fixtures of `plan.py`.
- `LocalizationTests` and `test_messages.py` fail on Cyrillic in code, or on a code without a text in the catalog. The engine's HTML report templates, the strategy's TOML configs and the groups in instrument seeds are data, not code, so Cyrillic remains there.
- A token glued to text is not translated. An end-to-end test checks that the report page contains no raw codes.
- A new language means a new catalog per owner; ADR-0027 adds English and the choice of language.
