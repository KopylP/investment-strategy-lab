# Strategy Builder 06 — Trial Backtests, Comparison, Publishing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The owner runs trial backtests of a draft (each an immutable version of one trial strategy), compares two runs, publishes the draft as a new strategy or a new version, and reopens a published `rule-based` strategy in the builder; trial strategies and their backtests disappear after publishing or discarding.

**Architecture:** Builder writes trial versions and publishes through `IStrategyAuthoring` (adapter over `IAuthoredStrategies`), reads backtest status through `ITrialBacktests` (adapter over `IBacktestStatusQueries`). Backtests run by the existing `Backtesting` path (same code for trial and published, §2 anti-goals). Cleanup that fails after a successful publish is retried by a scheduled job. The full result is shown by the existing `app-strategy-backtest` component.

**Tech Stack:** .NET 10, EF Core, `IScheduledJob`; Angular 21, PrimeNG, Chart.js through `shared/charts` (shared legend).

**Spec:** `docs/superpowers/specs/2026-11-03-strategy-builder-design.md` §6.5, §6.7, §7.6, §8.1, §9.1, §9.2, §9.4, §9.5, §10 (integration)

## Global Constraints

- All of the index plan's Global Constraints.
- A trial run with the same fingerprint (rules + setup + reports, plan 04 `DraftFingerprint`) never writes a new version; it returns the existing run.
- If publishing fails nothing is deleted; the trial strategy is deleted only after a successful publish or on discard.
- Trial results are visible only in the builder.

## Review Focus

1. «Бектест» pressed twice quickly → one trial version, not two (Task 1 test `A_repeated_run_returns_the_existing_trial`; the handler checks the fingerprint before writing).
2. Publishing succeeds but deleting the trial strategy fails → the draft is gone, a `TrialCleanup` row stays and the job deletes the trial later (Task 2 test `A_failed_cleanup_is_kept_for_the_job`).
3. Publishing a new strategy whose name the user already has → `strategy.name_taken` shown on the name, draft and trials untouched (Task 5 integration scenario asserts the draft still exists after a refused publish).
4. «Змінити в конструкторі» on a strategy that already has a draft → the existing draft opens, no second draft (Task 5 scenario posts `/published/P/versions/1/draft` twice and expects one draft id).
5. A trial backtest fails (not enough history) → the run shows the failure reason from the backtest, the comparison skips it (Task 3 manual check with a fund of 3 months' history).

---

### Task 1: Trial runs

**Files:**
- Modify: `backend/src/Modules/StrategyBuilder/InvestmentLab.StrategyBuilder/Drafts/Domain/IStrategyAuthoring.cs` (+ `WriteTrialVersionAsync`, `DeleteTrialAsync`, `PublishAsync`, `GetVersionAsync`)
- Create: `.../Drafts/Domain/AuthoredStrategy.cs`, `TrialVersion.cs`, `PublishedVersion.cs`, `AuthoredVersion.cs`
- Modify: `.../Drafts/Domain/StrategyDraft.cs` (`RecordTrial`, `TrialWith`)
- Modify: `.../Drafts/Infrastructure/StrategyAuthoring.cs`
- Create: `.../Trials/Domain/ITrialBacktests.cs`, `TrialStatus.cs`, `TrialSummary.cs`
- Create: `.../Trials/Infrastructure/TrialBacktests.cs`
- Create: `.../Trials/Application/RunTrial/RunTrialHandler.cs`, `.../Trials/Application/ListTrials/ListTrialsHandler.cs`
- Create: `.../Trials/Api/TrialEndpoints.cs`
- Modify: `backend/src/Modules/Strategies/InvestmentLab.Strategies.Contracts/IAuthoredStrategies.cs` (+ `GetVersionAsync`), create `AuthoredVersionDto.cs`; `.../Strategies/Definitions/Infrastructure/AuthoredStrategies.cs`
- Modify: `StrategyBuilderModule.cs`, `locales/uk.json`
- Test: `backend/tests/Modules/InvestmentLab.StrategyBuilder.Tests/Drafts/StrategyDraftTests.cs`

**Interfaces:**
- Consumes: `IAuthoredStrategies` (plan 03 Task 5), `IBacktestStatusQueries` (plan 03 Task 6), `RulesGenerator`, `DraftFingerprint`, `GraphCheck`, `EngineProblems`.
- Produces:
  - `public sealed record AuthoredStrategy(string Rules, DraftSetup Setup, IReadOnlyList<DraftReport> Reports)`; `public sealed record TrialVersion(Guid StrategyId, int Version)`; `public sealed record PublishedVersion(Guid StrategyId, int Version)`; `public sealed record AuthoredVersion(string Name, string Rules, DraftSetup Setup, IReadOnlyList<DraftReport> Reports)`.
  - `IStrategyAuthoring` members: `Task<Result<TrialVersion>> WriteTrialVersionAsync(UserId owner, Guid? trialStrategyId, AuthoredStrategy strategy, CancellationToken ct)`; `Task<Result> DeleteTrialAsync(Guid trialStrategyId, CancellationToken ct)`; `Task<Result<PublishedVersion>> PublishAsync(UserId owner, PublishedOrigin? origin, string name, AuthoredStrategy strategy, CancellationToken ct)`; `Task<AuthoredVersion?> GetVersionAsync(Guid strategyId, int version, CancellationToken ct)`. The adapter maps engine `Problems` of a refused write to `RulesCheck` problems so the handler can place them (failure code `strategy.parameters_refused` with a `RulesRefused` domain error carrying the problems).
  - Strategies contract addition: `Task<AuthoredVersionDto?> GetVersionAsync(Guid strategyId, int version, CancellationToken ct)` with `AuthoredVersionDto(string Name, string TypeCode, AuthoredSetupDto Setup)` (implemented by a `GetAuthoredVersion` query handler in `Definitions/Application/GetAuthoredVersion/`).
  - `StrategyDraft.RecordTrial(Guid trialStrategyId, int version, string fingerprint, string? label, DateTimeOffset now) -> TrialRun` (sets `TrialStrategyId` on the first run; numbers 1, 2, 3…); `StrategyDraft.TrialWith(string fingerprint) -> TrialRun?`.
  - `public enum TrialStatus { Waiting, Running, Succeeded, Failed }`; `public sealed record TrialSummary(TrialStatus Status, string? FailureReason, double? AnnualReturn, double? AnnualReturnAfterTax, double? Volatility, double? Sharpe, double? MaxDrawdown)`; `public interface ITrialBacktests { Task<TrialSummary> SummaryOfAsync(Guid strategyId, int version, CancellationToken ct); }` (no backtest yet → `Waiting`; `Queued` → `Waiting`; `NotSupported` → `Failed` with `builder.trial.not_supported`).
  - `RunTrialHandler.Handle(RunTrialCommand(Guid DraftId, string? Label), ct) -> Result<TrialRunDto>`: steps — load; local check (`GraphCheck` errors → `builder.trial.draft_has_problems`); generate; fingerprint; existing run → return it; write trial version (engine problems → `builder.trial.draft_has_problems` with the placed problems in the DTO); record; save.
  - `ListTrialsHandler.Handle(Guid draftId, ct) -> Result<IReadOnlyList<TrialRunDto>>` with `TrialRunDto(int Number, Guid StrategyId, int Version, string? Label, DateTimeOffset RanAt, string Status, string? FailureReason, double? AnnualReturn, double? AnnualReturnAfterTax, double? Volatility, double? Sharpe, double? MaxDrawdown)`.
  - Routes: `POST /api/strategy-builder/drafts/{id}/trials` `{ label? }`, `GET /api/strategy-builder/drafts/{id}/trials`.
  - Feature boundary (enforced by `DependencyRules`): `Trials` may use `Drafts/Domain` only. `TrialRunDto` and `TrialProblemDto(string Message, string Severity, string? NodeId)` live in `Trials/Application/Shared/`; never reuse `Drafts/Application` DTOs from `Trials`.

- [x] **Step 1: Write the failing tests**

```csharp
[Fact] public void A_repeated_run_returns_the_existing_trial() { /* RecordTrial(f1) then TrialWith(f1) returns it; TrialWith(f2) null */ }
[Fact] public void Trial_numbers_count_up_and_the_first_run_remembers_the_trial_strategy() { }
```

- [x] **Step 2: Run to verify they fail**

Run: `cd backend && dotnet test --project tests/Modules/InvestmentLab.StrategyBuilder.Tests`
Expected: FAIL.

- [x] **Step 3: Implement the contract addition in `Strategies`, the ports, adapters, handlers, endpoints and registrations**

- [x] **Step 4: Run tests**

Run: `dotnet test --project tests/Modules/InvestmentLab.StrategyBuilder.Tests && dotnet test --project tests/Modules/InvestmentLab.Strategies.Tests && dotnet test --project tests/InvestmentLab.ArchitectureTests`
Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add backend locales/uk.json
git commit -m "A draft can be backtested before it is published: each run becomes a fixed version of the draft's own trial strategy and is replayed by exactly the same backtest as a published strategy, running the same draft again shows the earlier run instead of making a new one, and the list of runs shows each one's state and headline numbers"
git pull --rebase && git push
```

---

### Task 2: Publishing, discarding, cleanup and opening a published strategy

**Files:**
- Create: `.../Drafts/Domain/PublishedLayout.cs`, `IPublishedLayoutRepository.cs`, `PublishedSource.cs`
- Create: `.../Drafts/Domain/TrialCleanup.cs`, `ITrialCleanupRepository.cs` (in `Drafts`, not `Trials`: the architecture tests let one feature use another only through its `Domain`, and publishing and discarding — both `Drafts` actions — start the cleanup)
- Create: `.../Drafts/Application/PublishDraft/PublishDraftHandler.cs`, `.../Drafts/Application/OpenPublishedStrategy/OpenPublishedStrategyHandler.cs`, `.../Drafts/Application/GetPublishedStrategy/GetPublishedStrategyHandler.cs`
- Modify: `.../Drafts/Application/DiscardDraft/DiscardDraftHandler.cs` (adds the trial's cleanup)
- Create: `.../Drafts/Application/CleanUpTrials/TrialCleanupJob.cs`, `CleanUpTrialHandler.cs`
- Create: `.../Drafts/Infrastructure/PublishedLayoutConfiguration.cs`, `PublishedLayoutRepository.cs`, `TrialCleanupConfiguration.cs`, `TrialCleanupRepository.cs`
- Modify: `.../Drafts/Api/DraftEndpoints.cs`, `StrategyBuilderModule.cs`, `locales/uk.json`
- Create: migration `PublishedLayoutsAndTrialCleanups`
- Test: `backend/tests/Modules/InvestmentLab.StrategyBuilder.Tests/Drafts/PublishedSourceTests.cs`, `.../Drafts/TrialCleanupTests.cs`

**Interfaces:**
- Produces:
  - `public sealed class PublishedLayout : AggregateRoot<Guid>` — `OwnerId`, `StrategyId`, `Version`, `StrategyGraph Graph`, `DraftLayout Layout`; `static PublishedLayout Of(UserId owner, PublishedVersion published, StrategyGraph graph, DraftLayout layout)`.
  - `public static class PublishedSource { public static Result<(StrategyGraph Graph, DraftLayout Layout)> Choose(PublishedLayout? saved, AuthoredVersion version, RuleVocabulary vocabulary); }` — the saved layout when it exists for that exact version, otherwise `GraphFromRules.Build(version.Rules)` with an empty layout.
  - `public sealed class TrialCleanup : AggregateRoot<Guid>` — `Guid TrialStrategyId`, `int Attempts`, `DateTimeOffset NextAttemptAt`; `void Failed(DateTimeOffset now)` (backoff: 5 min · 2^attempts, at most 1 day).
  - `PublishDraftHandler.Handle(Guid draftId, int revision, ct) -> Result<PublishedVersionDto>` steps: load; stale check; local check; generate; `IStrategyAuthoring.PublishAsync(owner, draft.Origin, draft.Name, strategy)` (engine problems → refused with placed problems; anything else → its error); on success in one save: add `PublishedLayout`, remove draft, add `TrialCleanup` when `TrialStrategyId` is set; then try `CleanUpTrialHandler.Handle(cleanupId)` once (its failure only logs).
  - `CleanUpTrialHandler.Handle(Guid cleanupId, ct)`: `DeleteTrialAsync`; success → remove the row; failure → `Failed(now)`.
  - `TrialCleanupJob : IScheduledJob` (interval 5 minutes): each due row in its own scope.
  - `OpenPublishedStrategyHandler.Handle(Guid strategyId, int version, ct) -> Result<DraftDto>`: an existing draft with this origin → it; otherwise `GetVersionAsync` → `PublishedSource.Choose` → `StrategyDraft.Start(..., origin)`.
  - `GetPublishedStrategyHandler.Handle(Guid strategyId, int version, ct) -> Result<PublishedStrategyDto>` with `PublishedStrategyDto(bool Editable, Guid? DraftId, IReadOnlyList<BlockWordsDto> Words)`; `Editable` = the version's type is `RuleBasedType.Code`; `Words` from `StrategyInWords` over the chosen source.
  - Routes: `POST /drafts/{id}/publish` `{ revision }`, `POST /published/{strategyId}/versions/{version}/draft`, `GET /published/{strategyId}/versions/{version}`.

- [x] **Step 1: Write the failing tests**

```csharp
[Fact] public void The_saved_layout_of_the_same_version_is_used() { }
[Fact] public void Another_version_is_rebuilt_from_its_rules() { }
[Fact] public void A_failed_cleanup_is_kept_for_the_job() { /* Failed twice -> Attempts 2, NextAttemptAt = now + 20 min */ }
[Fact] public void Cleanup_retries_wait_at_most_a_day() { }
```

- [x] **Step 2: Run to verify they fail; implement; run to verify they pass**

Run: `dotnet test --project tests/Modules/InvestmentLab.StrategyBuilder.Tests`
Expected: FAIL, then PASS. Then add the migration:
Run: `dotnet ef migrations add PublishedLayoutsAndTrialCleanups --project src/Modules/StrategyBuilder/InvestmentLab.StrategyBuilder --output-dir Shared/Infrastructure/Migrations`

- [x] **Step 3: Commit**

```bash
git add backend locales/uk.json
git commit -m "A draft can be published as a new strategy or as the next version of the strategy it came from; only after the strategy is saved does the draft go away together with its trial strategy and trial backtests, a failed clean-up is retried in the background, discarding a draft clears its trials too, and a published rule-based strategy opens again in the builder with its schema exactly as it was published"
git pull --rebase && git push
```

---

### Task 3: Trial runs and comparison on screen

**Files:**
- Create: `frontend/src/app/features/strategy-builder/pages/draft-trials-page/draft-trials-page.ts|html` (route `':draftId/trials'`, title «Бектести чернетки», back to the editor)
- Create: `.../data-access/trials-store.ts` (list, run, poll every 5 s while any run is `Waiting`/`Running`)
- Create: `.../ui/trial-comparison/trial-comparison.ts|html`
- Modify: `.../pages/draft-editor-page/*` («Бектест» runs `check` then `POST trials` and opens the trials page; problems stay in «Що перевірити»)
- Modify: `.../models/draft-dto.ts` (`TrialRunDto`)

**Interfaces:**
- Consumes: `POST/GET /api/strategy-builder/drafts/{id}/trials`; `GET /api/backtesting/strategies/{strategyId}?version=N` (existing, `BacktestDto` from `features/backtesting/models/backtest-dto.ts`); `app-strategy-backtest` (`features/backtesting/ui/strategy-backtest`) with `[strategyId]` and `[version]`; shared chart legend (`shared/charts/chart-legend`, `hidden-lines.ts`).
- Produces: the list (№, мітка, час, статус, річна дохідність, після податку, волатильність, Sharpe, найбільше падіння); selecting two runs shows `app-trial-comparison`: one table (rows: річна дохідність, після податку, волатильність, Sharpe, найбільше падіння; columns: запуск A, запуск B, різниця) and one growth chart with both strategy lines and run A's comparison lines; selecting one run shows `app-strategy-backtest` for it. A failed run shows its reason and cannot be chosen for comparison.

- [ ] **Step 1: Implement store, page, comparison; wire «Бектест»**
- [ ] **Step 2: Build**

Run: `cd frontend && npx ng build`
Expected: success.

- [ ] **Step 3: Check in the browser**

Run a trial of the template, change `top` to 2, run again, wait for both, compare; press «Бектест» again without changes (the same run opens); add a fund with three months of history and run (the run fails with its reason).
Expected: as described.

- [ ] **Step 4: Commit**

```bash
git add frontend
git commit -m "Trial backtests have their own page in the builder: runs are listed with their state and headline numbers and refresh while they are calculated, one run opens in the usual backtest view, and two runs are compared side by side in one table and one growth chart, so a change to the rules can be judged before it is published"
git pull --rebase && git push
```

---

### Task 4: Publishing on screen and the strategy page

**Files:**
- Modify: `.../pages/draft-editor-page/*` («Опублікувати»: `check` → confirm dialog «Опублікувати як нову стратегію "<назва>"?» or «Опублікувати версію N+1 стратегії "<назва>"?» → `publish` → navigate to `/strategies/{id}` with `replaceUrl`; problems land on nodes)
- Create: `frontend/src/app/features/strategy-builder/ui/published-strategy-panel/published-strategy-panel.ts|html` (inputs `strategyId`, `version`; output `editable: boolean`; shows «Стратегія словами» and «Змінити в конструкторі»)
- Modify: `frontend/src/app/features/strategies/pages/strategy-detail-page/strategy-detail-page.ts|html` (embeds the panel; hides the parameters form when `editable`)
- Modify: `frontend/src/app/features/strategies/pages/strategy-list-page/strategy-list-page.html` («Нова власна стратегія» → `/strategy-builder` with the new-draft dialog open, query `?new=1`)

**Interfaces:**
- Consumes: `POST /drafts/{id}/publish`, `GET /published/{strategyId}/versions/{version}`, `POST /published/{strategyId}/versions/{version}/draft`.

- [ ] **Step 1: Implement**
- [ ] **Step 2: Build**

Run: `npx ng build`
Expected: success.

- [ ] **Step 3: Check in the browser**

Publish the template draft: the strategy page opens with v1, «Стратегія словами» and «Змінити в конструкторі», no parameters form; the draft and its trials are gone from «Конструктор»; «Змінити в конструкторі» opens a draft with the same layout; change `top`, publish: v2 of the same strategy.
Expected: as described.

- [ ] **Step 4: Commit**

```bash
git add frontend
git commit -m "A draft is published from the builder after a confirmation that says whether it becomes a new strategy or the next version, and the page of a rule-based strategy now tells the strategy in sentences and offers «Змінити в конструкторі» instead of the parameters form, while the strategies list offers «Нова власна стратегія»"
git pull --rebase && git push
```

---

### Task 5: Integration scenario

**Files:**
- Create: `backend/tests/InvestmentLab.IntegrationTests/StrategyBuilder/StrategyBuilderScenarioTests.cs`

**Interfaces:**
- Consumes: `StrategiesApiFactory`, `SyntheticQuoteProvider`, `SignedInClient`, the waiting helper of `BacktestingScenarioTests` (`FinishedAsync`, `LongestReplay`); move `FinishedAsync` into a small shared helper class in the integration tests project if it is private.

- [ ] **Step 1: Write the scenario**

```csharp
[Fact]
public async Task A_draft_is_backtested_published_and_leaves_no_trial_behind()
{
    // ingest quotes; GET /api/strategy-builder/instruments; POST /drafts {name:"Моя стратегія", template:"momentum-trend-filter"}
    // PUT /drafts/{id} with funds (5 synthetic ETFs), benchmark, cash
    // POST /drafts/{id}/trials -> trial (strategyId T, version 1); POST again -> same number 1
    // GET /api/strategies -> does not contain T
    // wait until GET /api/backtesting/strategies/T?version=1 is Succeeded
    // POST /drafts/{id}/publish -> published (strategyId P, version 1)
    // GET /api/strategy-builder/drafts -> empty
    // GET /api/backtesting/strategies/T?version=1 -> 404 (trial and its backtests deleted; poll up to 30 s for the outbox)
    // wait until GET /api/backtesting/strategies/P?version=1 is Succeeded
    // POST /published/P/versions/1/draft twice -> the same draft id
    // a second draft named "Моя стратегія" published as new -> 409 strategy.name_taken; the draft still exists
}
```

- [ ] **Step 2: Run**

Run: `cd backend && dotnet test --project tests/InvestmentLab.IntegrationTests --filter "FullyQualifiedName~StrategyBuilderScenarioTests"` (Docker running)
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/InvestmentLab.IntegrationTests
git commit -m "An end-to-end test walks a draft through a trial backtest and publishing and checks that the trial strategy stays out of the strategies list, that it and its backtests are gone afterwards, that the published strategy gets its own backtest, that reopening it gives one draft, and that a refused publish keeps the draft"
git pull --rebase && git push
```

## Execution status — 2026-11-03

Tasks 1–2 are complete. Publishing saves the exact version's graph and layout, removes the draft only after successful authoring, and durably queues trial cleanup. Cleanup failure leaves the published strategy intact, records backoff, and can be retried; discarded drafts queue the same cleanup job. Published rule-based strategies reopen with their saved layout and reuse an existing draft. Refused duplicate-name publication preserves the draft. The migration is generated. Task 2 verification: StrategyBuilder (94), architecture (50), and publishing/cleanup integration scenarios (2) pass. Task 1's real-engine trial scenario and schema-alternative regression were verified in its own commit. Tasks 3–5 remain open. Plans remain tracked at the user's explicit request.
