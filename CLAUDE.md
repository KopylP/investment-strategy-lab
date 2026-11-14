# Investment Strategy Lab

Personal investment service: accounts split into sub-portfolios, each run by a strategy; the service makes reports
(rebalance plans, risk reviews) the user executes at their broker and marks as done. Deployed as one Home Assistant add-on.

| Folder | What | Own notes |
|---|---|---|
| `backend/` | .NET 10 modular monolith + Aspire AppHost (starts Postgres, the API, the strategy engine, the frontend) | `backend/CLAUDE.md` |
| `frontend/` | Angular 21 + PrimeNG + Tailwind SPA | `frontend/CLAUDE.md` |
| `strategy-engine/` | Python `finance_decisions`: strategies behind a FastAPI HTTP API | `strategy-engine/CLAUDE.md` |
| `deploy/home-assistant/` | The add-on: one container with Postgres + engine + API serving the SPA | `deploy/home-assistant/CLAUDE.md` |
| `docs/architecture/` | `adr/` (architecture decisions); domain terms live in `docs/domain/glossary.md` | — |

## Commands

```bash
# Whole stack (needs Docker for Postgres). It does not install frontend packages: run `cd frontend && npm ci` first
# and whenever package-lock.json changes, never a bare `npm install` (on macOS it drops packages Linux CI needs)
cd backend && dotnet run --project src/Hosts/InvestmentLab.AppHost --launch-profile https

cd backend && dotnet build InvestmentLab.slnx && dotnet test --solution InvestmentLab.slnx   # integration tests need Docker
cd strategy-engine && uv run pytest -q
cd frontend && npx ng build
```

## How the pieces talk

- Strategy types live in the engine; the backend syncs their catalog (`GET /v1/strategies`) and asks them for
  decisions per sub-portfolio. The contract is `strategy-engine/contracts/v1/openapi.json`: a change on one side
  usually needs the other side (backend `Strategies` module, frontend models) in the same change.
- The frontend's API types are generated from the backend's OpenAPI document, `backend/contracts/openapi.json`, written
  by a Debug build of the API; regenerate them with `npm run api:types` in `frontend/` and commit both (CI checks it).
- Reports a strategy type declares (code, name, mode, default schedule) drive what the backend schedules and generates
  and what the frontend lists — nothing about a specific report is hard-coded elsewhere.
- The same holds for what a backtest is compared with (ADR-0017): the type declares comparison codes, each is one
  `IReplayComparison` block in `Backtesting`, and the screen draws whatever series come back.
- Reports are generated in the background (ADR-0005): a `ReportRequest` polls the strategies, then the report is
  published as an integration event; Notifications turns events into Home Assistant notices.

## Conventions

- Everything the user sees is in **Ukrainian and English** (UI, report texts, notifications); the user picks the language in
  the profile, the browser's language is the default (ADR-0027). Add-on docs stay Ukrainian. MCP replies are always English.
  Code, identifiers and code comments are in English. A report is «рапорт» in the Ukrainian UI.
- **No text in the user's language in backend or engine code.** See Language below.
- Commit messages and code follow the code rules below; the why of a decision goes in an ADR, not in a comment.
  ADRs (`docs/architecture/adr/`) are written in English.
- Commit messages: one descriptive paragraph of what changed for the user and why (see `git log`), no type prefixes.
- Do not bump `deploy/home-assistant/investlab/config.yaml` `version` by hand: every push to `main` that touches `backend/`,
  `frontend/`, `strategy-engine/` or the add-on is released by a workflow, which builds the add-on image, pushes it to
  GHCR and then commits the next version (ADR-0028). Backend pushes are also auto-formatted by `dotnet format` and
  committed back — pull before pushing again.

## Code rules (Clean Code)

New and changed code follows these rules; they win over the style of older code around it.

- **The framework first.** What ASP.NET Core, EF Core, the SDK in use or another library we already have does
  (authentication schemes, Bearer parsing, challenges, validation, caching, serialization), we use and configure. Own code
  only fills the one place where the standard configuration falls short, plugged into the framework's extension point
  (a `TokenHandler`, an events class, an options callback), never a hand-written copy of what the framework does.
- **Code documents itself.** No `<summary>` or doc comments and no comments that narrate. If code needs explaining,
  rename or extract until it does not.
- **One action, one handler.** Each use case has its own handler in its own folder (`Application/CreateSignal/`). No
  handler class with `Get`, `Create` and `Delete` methods. An integration event handler reacts to one event.
- **Small functions, one thing each,** at one level of abstraction. A function reads as steps; the details live in the
  functions it calls.
- **Every cohesive block that does one thing is its own function.** A long `if` condition or a LINQ chain moves to a
  function whose name says what it finds or decides.
- **Blank lines separate logical segments:** guards, then gathering data, then deciding, then saving, then returning.
- **No dense expressions.** Never nest construction, LINQ and formatting in one statement. Name intermediate results.
  Constructors with many arguments use named arguments, one per line.
- **One type per file,** except a handler with the small command and DTO records only it uses.
- **No orphan files.** Every file sits where the structure says, in every change, including mechanical ones and work
  done by an agent: a handler or job in its action's folder (`Application/<ActionName>/`), shared types in a named folder
  or in `Domain`, never loose in `Application/`. A file's name says what it holds; a file emptied by a split is deleted.
- **Small types over long parameter lists.** Group arguments that travel together into a value object.
- **Intention-revealing names,** no abbreviations; named constants instead of magic numbers.
- **A type is named for what it is in the domain, not for its role in the code.** `Input`, `Inputs` say nothing and are not used:
  what a user asks to create is `NewSubPortfolio`, what they choose is `ContributionPolicyChoice`, an unfinished thing is
  `StrategySetupDraft`, what a calculation works from is `SubPortfolioSituation`. A static class that parses and validates a request
  is split: each rule goes to the domain type it builds (`SubPortfolioDraft.TryOf`). `ForeignContractIsolationTests` keeps the suffix out.

## Tools

- **C# code is navigated through the LSP first.** References, definitions, implementations, call hierarchy and symbol search go
  through the LSP tool (`csharp-ls`: `dotnet tool install -g csharp-ls`); a rename or a move starts from `findReferences`. Text search is
  for what the compiler does not see: strings, JSON, Markdown, frontend code. An empty LSP answer while the server is still
  indexing proves nothing: then fall back to text search and let the build confirm it.

## Language

The backend and the engine speak in codes; the texts live in catalogs and are chosen where the user reads them.

- **A message is a code with parameters:** `Message.Of("report.warning.turnover", new { turnover, limit })` makes the
  token `@report.warning.turnover {"turnover":0.4,"limit":0.2}`. It travels and is stored as a plain string. Pass raw values
  (fractions, amounts, a currency code) and let the template format them; a parameter may itself be a message.
- **An error is a code too:** `Error.Validation(code, englishDetail, parameters)`. The English detail is for logs and developers.
- **Texts live in catalogs, one per language with the same codes:** `locales/{uk,en}.json` for the service, and a strategy's
  own `locales/{uk,en}.json` in the engine for its texts. A new text goes into every language in the same change.
  Templates use `{name}`, `{x:percent}`, `{x:signedPercent}`, `{x:money:currencyParam}`, `{x:amount}`, `{x:plural:one|few|many}`,
  `{x:join:separator}`. Never glue a message into other text (`$"{name}: {note}"`, `"; ".join(...)`): it would show as a raw code. Pass it as a parameter.
- **The frontend localizes:** `core/i18n/translation-interceptor.ts` translates every token in every API response into the
  active language, so a screen shows the backend's strings without knowing about codes. The screens' own texts are Transloco
  keys in `frontend/src/i18n/{uk,en}.json` (ICU syntax), and every API request carries `Accept-Language`. A string without a known code is shown as it is, so older
  stored texts and what the user typed keep working.
- **The backend renders only what never reaches the frontend:** Home Assistant notifications and generated PDFs go through
  `MessageLocalizer`, in the adapter or printer, reading the same catalogs: a PDF in the request's language (ASP.NET Core
  request localization), a notification in the user's chosen language or Home Assistant's, MCP replies in English.
- **Official names stay:** Polish field names on a PIT form are the form's own words, not translations.
- Enforced by `LocalizationTests`: no Cyrillic in backend or frontend code (PrimeNG's own Ukrainian texts excepted), every code
  the backend sends exists in the catalog, and every language of a catalog has the same codes.

## Modularity

Whatever can change from the outside stays replaceable: changing it touches its one owner and nothing else. This is a
requirement for every change, also a small one, and it wins over the shortest way to finish.

| What can change | Its only owner | Everyone else |
|---|---|---|
| Source of quotes and symbols (today Yahoo) | `MarketData` domain and adapter (`QuoteProvider`, `ISymbolConvention`) and the engine's `market_data.py` | Uses the neutral `InstrumentDto.Ticker` and `Label`; never parses or names a provider |
| Exchange rates and their pivot currency (today NBP, PLN) | `MarketData` (`PrimaryFxSource`, `IFxRateProvider`) | Asks `IFxRates.GetCrossRate`; never converts through a currency of its own |
| Tax rules of a country or account type | A policy in `Taxation` (`ITaxPolicy` and its blocks: rates, taxable gain, currency and FX rule, losses, lots, rounding, texts) | Asks the policy, the frontend through `/api/taxation/regime` and `/policies`; never has a rate, a form name or a currency of its own. A declaration form is Polish by nature and stays with its form |
| Strategies and their reports | The strategy type in the engine | Reads what the type declares; nothing about one report is hard-coded |
| Any other outside service or standard | An interface in the owning module with its adapter in `Infrastructure` | Depends on the interface or the `.Contracts` query |

- **A DTO of another module never leaves the class that reads it,** and no class gathers other modules' data for handlers. When to read a
  contract directly and when to use a port with an adapter: see Data from other modules in `backend/CLAUDE.md`.
- **Never spread knowledge of a provider or a country:** no prefixes, suffixes, form names, rates or provider names outside
  the owner. When a second place needs that knowledge, add it to the owner's contract instead of copying it.
- **An unknown value fails loudly.** A missing policy or provider gives an error, never a silent default that produces
  believable but wrong numbers.
- **A new variant is a new block:** adding one is a new class and its registration, without `if`s around the code that uses it.
- **Keep it enforced:** when a boundary can be checked, add a check to `InvestmentLab.ArchitectureTests`
  (`ProviderNeutralityTests` is the example).
- Do not build for a variant nobody needs yet: keep the seam, leave the unused implementation out.
