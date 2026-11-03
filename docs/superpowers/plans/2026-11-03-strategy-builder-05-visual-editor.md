# Strategy Builder 05 — Visual Editor («Конструктор») Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The owner lists drafts, starts one (empty, from a template, from a file) and edits it in three modes over one draft — «Схема» (Rete graph), «Кроки» (guided steps), «JSON» (read-only view with export/import) — with autosave, «Стратегія словами» and «Що перевірити».

**Architecture:** New feature `frontend/src/app/features/strategy-builder/` that reads only the Builder API (`/api/strategy-builder/...`). The draft store owns the draft DTO and autosaves; the Rete editor is a view that turns user gestures into a new graph for the store. All rules (checks, sentences, sockets, "copy entry to exit") come from the backend; the frontend only lays out and formats.

**Tech Stack:** Angular 21 zoneless, signals, PrimeNG 21 (Aura), Tailwind; Rete.js v2 with the plugins and renderer chosen in plan 01 (see index "Spike outcome").

**Spec:** `docs/superpowers/specs/2026-11-03-strategy-builder-design.md` §8.1–§8.4, §6.3 (autosave), mockup `rules-graph-v3.html`

## Global Constraints

- All of the index plan's Global Constraints.
- `frontend/CLAUDE.md`: standalone components, `OnPush`, `input()`/`computed`/`resource()`, `@if`/`@for`; stores `@Injectable()` provided by their page; DTOs in `models/*-dto.ts`, view models `*-vm.ts`, mapping in `data-access/*-mapper.ts`; no business logic in the frontend.
- All visible text Ukrainian; node font 13 px, captions ≥ 12 px, no abbreviations (§8.2). Socket types differ by shape: circle number, diamond yes/no, square fund list; primary colour only for actions and selection.
- Autosave: 800 ms after the last change of rules/setup/reports (`PUT /drafts/{id}` with the current `revision`), 1 500 ms after the last move (`PUT /drafts/{id}/layout`). A `builder.draft.stale` answer reloads the draft and shows a toast.
- Verification is `npx ng build` plus a manual check in the browser with the AppHost running; there are no unit specs in the frontend.

## Review Focus

1. Switching modes loses the selected node or unsaved edits → selection is kept in the store (`selectedNodeId`) and a pending autosave flushes before the mode switch (Task 1 store `flush()`; checked manually in Task 5 Step 3).
2. Undo after an autosave restores a graph the server has already replaced → undo produces a new graph and is saved like any change (Task 2 Step 4 manual check: change, wait 1 s, Ctrl+Z, reload page shows the undone state).
3. Nodes from an import or an old version have no positions → auto-arranged inside their block; existing manual positions never move (Task 2 Step 4 manual check with an imported file).
4. A wire dropped on empty canvas offers a node whose input type does not match → only compatible kinds from `vocabulary.nodeKinds` are listed (Task 3 manual check).
5. A long draft name or a 60-fund setup breaks the header or the setup row on a narrow screen → text truncates with a tooltip (Task 1 manual check at 1024 px).

---

### Task 1: Feature skeleton, drafts list, editor shell, setup and reports

**Files:**
- Create: `frontend/src/app/features/strategy-builder/strategy-builder.routes.ts`
- Modify: `frontend/src/app/app.routes.ts` (path `strategy-builder`), `frontend/src/app/core/layout/app-menu.ts` (item «Конструктор», icon `pi pi-fw pi-sitemap`, after «Стратегії»)
- Create: `.../models/draft-dto.ts`, `vocabulary-dto.ts`, `instrument-dto.ts`, `draft-vm.ts`
- Create: `.../data-access/strategy-builder-api.ts`, `drafts-store.ts`, `draft-store.ts`, `draft-mapper.ts`
- Create: `.../pages/draft-list-page/draft-list-page.ts|html`, `.../pages/draft-editor-page/draft-editor-page.ts|html`
- Create: `.../ui/setup-row/setup-row.ts`, `.../ui/setup-dialog/setup-dialog.ts|html`, `.../ui/reports-dialog/reports-dialog.ts|html`, `.../ui/new-draft-dialog/new-draft-dialog.ts`

**Interfaces:**
- Consumes: Builder API of plan 04 Task 7 (`DraftDto`, `VocabularyDto`, instruments), `app-report-timing-editor` (`features/reports/ui/report-timing-editor`), `app-page-header`, `Viewport`.
- Produces:
  - Routes: `''` → `DraftListPage` (title «Конструктор»), `':draftId'` → `DraftEditorPage` (title «Чернетка»).
  - `StrategyBuilderApi` (providedIn root): `vocabulary()`, `instruments()`, `drafts()`, `create(name: string, template: string | null)`, `get(id)`, `change(id, change: DraftChangeDto)`, `moveNodes(id, layout: LayoutDto)`, `check(id)`, `rules(id)`, `importRules(name, json)`, `discard(id)`.
  - `DraftStore` (`@Injectable()`, provided by `DraftEditorPage`): signals `draft`, `vocabulary`, `instruments`, `mode: 'steps' | 'graph' | 'json'`, `selectedNodeId`, `saving: 'saved' | 'pending' | 'saving'`; commands `open(id)`, `changeGraph(graph: GraphDto)`, `changeSetup(setup)`, `changeReports(reports)`, `rename(name)`, `moveNodes(layout)`, `select(nodeId | null)`, `setMode(mode)` (calls `flush()` first), `flush(): Promise<void>`, `check()`. Debounce timers are the two constants `RULES_SAVE_DELAY_MS = 800`, `LAYOUT_SAVE_DELAY_MS = 1500`. After each save the store replaces `problems` and `words` from the answer.
  - Editor header: back link to the list (`backLink`, `backLabel` «Конструктор»), name editable in place, status «Чернетка · збережено» / «Чернетка · зберігаю…», mode switch «Кроки / Схема / JSON», buttons «Сьогодні» (disabled until plan 09), «Бектест» and «Опублікувати» (disabled until plan 06). Below: setup row «Фонди N (перші 6 міток +N) · Бенчмарк · Кеш · Рапорти: …» with «Змінити».
  - Setup dialog: fund multi-select (filter, grouping by `group`), benchmark, cash, volatility index (optional), extra series (optional), each from `/instruments`; the 60-fund limit is enforced by the backend message, the dialog only shows the counter «N із 60».
  - Reports dialog: for each declared report code, `app-report-timing-editor` and built-in signal kinds checkboxes (labels from the existing signal labels in `features/signals/models/signal-labels.ts`; user signals come in plan 07).
  - List page: table (назва, з якої стратегії, остання зміна, останній пробний запуск), «Нова стратегія» opening `NewDraftDialog` (empty or a template from `vocabulary.templates`, name required) and «Імпортувати JSON» (file input → `importRules`).

- [ ] **Step 1: Create models, API, mapper and stores**
- [ ] **Step 2: Create the list page, the editor shell, the setup row and both dialogs**
- [ ] **Step 3: Build**

Run: `cd frontend && npx ng build`
Expected: success.

- [ ] **Step 4: Check in the browser**

Start the AppHost; open «Конструктор», create «Моментум із фільтром тренду», set funds, benchmark and cash, change a report time, wait 1 s and reload.
Expected: the draft reloads with the same setup and reports; the header shows «збережено»; at 1024 px a long name truncates.

- [ ] **Step 5: Commit**

```bash
git add frontend
git commit -m "A new «Конструктор» section lists strategy drafts and starts new ones, empty, from a template or from a rules file; a draft opens with its name, the mode switch and a row with its funds, benchmark, cash fund and reports, which open in dialogs, and every change is saved by itself shortly after the last edit"
git pull --rebase && git push
```

---

### Task 2: The schema canvas

**Files:**
- Modify: `frontend/package.json`, `frontend/package-lock.json` (Rete packages at the versions in the index "Spike outcome")
- Create: `.../ui/graph/graph-renderer.ts` (the one place that depends on the spike outcome)
- Create: `.../ui/graph/graph-editor.ts|html` (component `app-graph-editor`)
- Create: `.../ui/graph/graph-schemes.ts` (Rete node/connection classes built from `GraphDto` and `vocabulary.nodeKinds`)
- Create: `.../ui/graph/block-frame/block-frame.ts` (scope parent: title, purpose line «для кожного фонду в портфелі», collapsed sentence, «Впорядкувати»)
- Create: `.../ui/graph/nodes/indicator-node.ts`, `compare-node.ts`, `logic-node.ts` (all/any/not), `range-node.ts` (between, crossed), `signal-node.ts`, `result-node.ts`
- Create: `.../ui/graph/sockets/socket-shape.ts` (circle / diamond / square by `SocketType`)
- Create: `.../data-access/graph-mapper.ts` (`GraphDto` ↔ editor model, positions relative to the block)

**Interfaces:**
- Consumes: `DraftStore` (Task 1), `vocabulary.nodeKinds` (plan 04 Task 7).
- Produces:
  - `GraphRenderer.attach(area: AreaPlugin<Schemes, AreaExtra>, injector: Injector): void` — angular plugin with presets, or own components, per the spike.
  - `app-graph-editor` inputs: `graph: GraphDto`, `layout: LayoutDto`, `vocabulary: VocabularyVm`, `problems: DraftProblemVm[]`, `selectedNodeId: string | null`, `words: BlockWordsVm[]`; outputs: `graphChange: GraphDto`, `layoutChange: LayoutDto`, `select: string | null`.
  - Blocks are scope parents in `BlockKind` order top to bottom: ① ринковий фільтр, ② вихід, ③ вхід, ④ відбір і ваги, ⑤ ризик між ребалансами; each shows its result as a grey plate on the right with the honest caption («Ринок сприятливий: так → 100 %, ні → 30 %, решта в IB01», «Продати», «Кандидат на купівлю», «Цільові ваги»). A collapsed block shows only its sentence from `words`; only the edited block is expanded by default.
  - Nodes show their parameters (the indicator shows «для чого»: «цей фонд», «SPY (бенчмарк)»…; a compare shows «A [оператор] B» with what is connected to each input; a signal shows the «активний / не активний» switch); editing happens in the inspector (Task 3).
  - Dragging snaps to a 16 px grid; nodes cannot leave their block (scopes plugin); links can only be drawn inside one block and between matching socket types (incompatible sockets dim while dragging a wire).
  - History plugin: Ctrl+Z / Ctrl+Shift+Z; every resulting graph goes to `graphChange` (saved like any edit).
  - Auto-arrange (`rete-auto-arrange-plugin`) runs per block for nodes without a position and on «Впорядкувати»; nodes with positions never move otherwise.

- [ ] **Step 1: Install the packages**

Run: `cd frontend && npm install <packages@versions from the index "Spike outcome">`
Expected: lockfile updated; `angular.json` polyfills unchanged (no `zone.js`).

- [ ] **Step 2: Implement renderer, schemes, mapper, block frame, node and socket components, and `app-graph-editor`; mount it in the editor page for mode `graph`**
- [ ] **Step 3: Build**

Run: `npx ng build`
Expected: success; `grep -l "__zone_symbol__" dist/frontend/browser/*.js` prints nothing.

- [ ] **Step 4: Check in the browser**

Open the template draft in «Схема»: five blocks in order, result plates on the right, sockets in three shapes; drag a node (snaps, stays in its block); connect a number into a yes/no input (refused); change, wait 1 s, Ctrl+Z, reload (the undone state is saved); import the JSON of a template from Task 6 later or via `POST /drafts/import` now (nodes auto-arranged).
Expected: all as described.

- [ ] **Step 5: Commit**

```bash
git add frontend
git commit -m "Drafts can be edited as a schema: the five blocks of a strategy stand from top to bottom in the order the strategy runs them, each with its result on the right, nodes show what they look at and compare, sockets differ by shape for numbers, yes-or-no and fund lists, wires only join what fits, nodes snap to a grid and stay in their block, undo and redo work with the keyboard, and nodes without a place are arranged by themselves"
git pull --rebase && git push
```

---

### Task 3: Inspector, palette and quick edits

**Files:**
- Create: `.../ui/inspector/inspector.ts|html` (fields from the vocabulary: integer, number, fraction as percent, series choice, choice; hint and phrase; list of links)
- Create: `.../ui/palette/palette.ts` («Додати блок» per block; indicators and conditions from the vocabulary)
- Create: `.../ui/graph/compatible-nodes-menu.ts` (opens where a wire is dropped on empty canvas; filters `nodeKinds` by the dragged socket type; inserts and connects)
- Create (backend): `backend/src/Modules/StrategyBuilder/InvestmentLab.StrategyBuilder/Drafts/Domain/ExitFromEntry.cs`
- Create (backend): `.../Drafts/Application/CopyEntryToExit/CopyEntryToExitHandler.cs`, route `POST /drafts/{id}/copy-entry-to-exit` `{ margin }` in `DraftEndpoints.cs`
- Test: `backend/tests/Modules/InvestmentLab.StrategyBuilder.Tests/Drafts/ExitFromEntryTests.cs`

**Interfaces:**
- Consumes: `RuleVocabulary`, `StrategyGraph`.
- Produces:
  - `public static class ExitFromEntry { public static StrategyGraph Of(StrategyGraph graph, RuleVocabulary vocabulary, double margin); }` — replaces the Exit block with the negation of the Entry block's condition: `compare gt/gte value v` → `compare lt (v − margin)`, `lt/lte v` → `gt (v + margin)` — the margin applies only when the left indicator's `ColumnFormat` is `percent` or `signedPercent`, otherwise 0; compare against another indicator → opposite operator, no margin; `between`/`crossed` → `not(…)`; `signal` → opposite `state`; `all` → `any` of the negated children; `any` → `all`; `not(x)` → `x`. New node ids `x-1`, `x-2`…; indicator nodes are copied into the Exit block.
  - `CopyEntryToExitHandler.Handle(Guid draftId, int revision, double margin, ct) -> Result<DraftDto>` (`margin` must be 0..0.5, else `builder.copy.margin_out_of_range`).
  - UI: «Скопіювати вхід у вихід із запасом» in the Exit block menu opens a small dialog with the margin (default 2 %); «Дублювати» on a node; double-click on a wire opens the compatible-nodes menu and inserts the node into the wire.

- [ ] **Step 1: Write the failing backend tests**

```csharp
[Fact] public void A_positive_momentum_entry_becomes_a_negative_momentum_exit_with_the_margin() { /* entry mom > 0, margin 0.02 -> exit mom < -0.02 */ }
[Fact] public void All_becomes_any_of_the_negations() { }
[Fact] public void A_comparison_of_two_indicators_flips_without_a_margin() { }
[Fact] public void A_price_level_gets_no_margin() { /* price > 100 -> price < 100 */ }
[Fact] public void A_signal_condition_flips_its_state() { }
[Fact] public void Not_is_removed_instead_of_doubled() { }
```

- [ ] **Step 2: Run to verify they fail; implement `ExitFromEntry` and the handler; run to verify they pass**

Run: `cd backend && dotnet test --project tests/Modules/InvestmentLab.StrategyBuilder.Tests`
Expected: FAIL, then PASS.

- [ ] **Step 3: Implement the inspector, palette, compatible-nodes menu, duplicate and the copy dialog; build**

Run: `cd frontend && npx ng build`
Expected: success.

- [ ] **Step 4: Check in the browser**

Drop a wire from an indicator output on empty canvas: only nodes with a number input are offered; double-click a wire and insert «НЕ» into a yes/no wire; duplicate a compare; copy entry to exit with 2 %.
Expected: as described; the inspector shows the engine's hint for the selected node.

- [ ] **Step 5: Commit**

```bash
git add backend frontend
git commit -m "Nodes are edited in an inspector beside the schema with the engine's explanation of each field, new nodes come from a palette per block or from a menu that opens where a wire is dropped and offers only nodes that fit, a node can be inserted into a wire or duplicated, and the exit block can be filled from the entry rules turned around with a safety margin, so a fund is not sold the day after it barely stops qualifying"
git pull --rebase && git push
```

---

### Task 4: «Стратегія словами» and «Що перевірити»

**Files:**
- Create: `.../ui/words-panel/words-panel.ts` (one sentence per block from `draft.words`)
- Create: `.../ui/problems-panel/problems-panel.ts` (problems only; click selects the node and switches to its block; problems of reports open the reports dialog)
- Modify: node components (Task 2) — a problem badge (error red, warning amber) with the message as tooltip
- Modify: `draft-editor-page.html` (right column on wide screens; below the content on narrow ones)

**Interfaces:**
- Consumes: `DraftStore.draft().words`, `.problems`, `select(nodeId)`.

- [x] **Step 1: Implement the two panels and node badges**
- [x] **Step 2: Build**

Run: `npx ng build`
Expected: success.

- [x] **Step 3: Check in the browser**

Break the template (delete the entry compare's left input): «Що перевірити» lists «немає входу …», the node has a red badge, clicking the problem selects it; «Стратегія словами» changes after the save.
Expected: as described.

- [x] **Step 4: Commit**

```bash
git add frontend
git commit -m "Beside the schema the draft is told in sentences, one per block, and a list says only what needs fixing; clicking a problem selects its node, which also carries a mark, so a broken strategy is visible at a glance"
git pull --rebase && git push
```

---

### Task 5: «Кроки»

**Files:**
- Create: `.../ui/steps/steps-view.ts|html` (progress indicator, free navigation, optional steps skippable)
- Create: `.../ui/steps/condition-rows.ts` (a block's condition as rows «індикатор · оператор · число/індикатор», AND/OR of rows)
- Create: `.../ui/steps/readonly-condition.ts` (sentence + «Редагувати в схемі»)
- Create: `.../data-access/condition-rows-mapper.ts` (graph block ↔ rows; `null` when the block is not row-shaped)

**Interfaces:**
- Consumes: `DraftStore`, `words`, vocabulary.
- Produces: steps in order (§8.3): 1 «Назва й опис», 2 «Фонди й ряди» (setup dialog content inline), 3 «Ринковий фільтр», 4 «Вихід», 5 «Вхід», 6 «Відбір і ваги», 7 «Ризик між ребалансами» (optional), 8 «Рапорти», 9 «Перевірка й бектест» (runs `check`, lists problems; «Бектест» arrives in plan 06). A block is row-shaped when its condition is a single compare/between/crossed/signal, or one `all`/`any` of such nodes; anything deeper (nested OR/NOT) shows `readonly-condition` and nothing is simplified. Rows map back to the same node ids, so layout and selection survive mode switches.

- [x] **Step 1: Implement the mapper, rows, read-only condition and the steps view**
- [x] **Step 2: Build**

Run: `npx ng build`
Expected: success.

- [x] **Step 3: Check in the browser**

Edit the entry threshold in «Кроки», switch to «Схема»: the same node is selected and shows the new value; build a nested OR/NOT in the schema and return to «Кроки»: the step shows the sentence and «Редагувати в схемі».
Expected: as described.

- [x] **Step 4: Commit**

```bash
git add frontend
git commit -m "A draft can also be built step by step, from the name and funds through each block to reports and the check, with simple conditions edited as rows; a condition too complex for rows is shown as a sentence with a way into the schema instead of being simplified, and the selected element stays selected when switching between steps and schema"
git pull --rebase && git push
```

---

### Task 6: «JSON»

**Files:**
- Create: `.../ui/json-view/json-view.ts` (read-only, highlighted by a small token colouriser in the component — no new dependency; «Копіювати», «Експортувати», «Імпортувати»)
- Modify: `draft-store.ts` (`rules()` loads `GET /drafts/{id}/rules`)

**Interfaces:**
- Consumes: `GET /drafts/{id}/rules` (generated JSON), `POST /drafts/import`.
- Produces: export downloads `<draft name>.json`; import is refused only for what cannot become a graph (not JSON, unknown field, kind or reference — listed with their paths); anything else opens as a new draft whose problems (e.g. a value out of bounds) sit on their nodes, and selecting such a problem opens «Схема» with that node selected. No in-place text editing.

- [x] **Step 1: Implement**
- [x] **Step 2: Build**

Run: `npx ng build`
Expected: success.

- [x] **Step 3: Check in the browser**

Export the template's JSON; add `"foo": 1` inside `selection` and import: refused with the path `selection.foo`; change `"top": 3` to `"top": 99` and import: a new draft opens with auto-arranged nodes and a problem «поза межами» on the selection result.
Expected: as described.

- [x] **Step 4: Commit**

```bash
git add frontend
git commit -m "The rules a draft generates can be read as highlighted JSON, copied and exported to a file, and a rules file can be imported into a new draft after a check that names the place of every problem; the JSON is never edited by hand, so a draft always stays something the engine can run"
git pull --rebase && git push
```

## Execution status — 2026-11-03

Tasks 4–6 implemented and committed locally. Production Angular build passes. Playwright checks guided edits, autosave and reload, selection across modes, deliberate missing-input problem navigation and badge, nested logic read-only fallback, JSON export, rejection of `selection.foo`, import of `top: 99`, localized words after reload, and the 1024 px layout. Tasks 1–3 predate this continuation; their remaining manual checks have not been marked complete here.
