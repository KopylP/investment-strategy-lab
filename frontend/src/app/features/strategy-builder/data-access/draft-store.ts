import { OwnSignalDto } from '../models/own-signal-dto';
import { PublishedVersionDto } from '../models/published-version-dto';
import { TrialRunDto } from '../models/trial-run-dto';
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { translateDeep } from '../../../core/i18n/translate';
import { problemCode, toProblemMessage } from '../../../core/api/api-client';
import {
  DraftDto,
  DraftReportDto,
  DraftRulesDto,
  DraftSetupDto,
  GraphDto,
  LayoutDto,
  DraftProblemDto,
} from '../models/draft-dto';
import { EditorMode, SaveState } from '../models/draft-vm';
import { InstrumentChoiceDto } from '../models/instrument-dto';
import { VocabularyDto } from '../models/vocabulary-dto';
import { StrategyBuilderApi } from './strategy-builder-api';

export const RULES_SAVE_DELAY_MS = 800;
export const LAYOUT_SAVE_DELAY_MS = 1500;

@Injectable()
export class DraftStore {
  private readonly api = inject(StrategyBuilderApi);
  private readonly messages = inject(MessageService);
  private readonly currentDraft = signal<DraftDto | null>(null);
  private readonly currentVocabulary = signal<VocabularyDto | null>(null);
  private readonly currentInstruments = signal<InstrumentChoiceDto[]>([]);
  private readonly currentSignals = signal<OwnSignalDto[]>([]);
  private readonly currentMode = signal<EditorMode>('graph');
  private readonly currentSelection = signal<string | null>(null);
  private readonly saveState = signal<SaveState>('saved');
  private rulesTimer: ReturnType<typeof setTimeout> | null = null;
  private layoutTimer: ReturnType<typeof setTimeout> | null = null;
  private rulesDirty = false;
  private layoutDirty = false;
  private changeNumber = 0;
  private discarding = false;
  private rulesSave: Promise<void> | null = null;
  private layoutSave: Promise<void> | null = null;
  private openNumber = 0;

  readonly draft = this.currentDraft.asReadonly();
  readonly vocabulary = this.currentVocabulary.asReadonly();
  readonly instruments = this.currentInstruments.asReadonly();
  readonly signals = this.currentSignals.asReadonly();
  readonly mode = this.currentMode.asReadonly();
  readonly selectedNodeId = this.currentSelection.asReadonly();
  readonly saving = this.saveState.asReadonly();
  readonly busy = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly saveConflict = signal(false);
  readonly reviewVisible = signal(false);
  readonly nameError = signal<string | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal<unknown>(null);
  readonly reportNames = computed(() =>
    Object.fromEntries(
      (this.currentVocabulary()?.reports ?? []).map((report) => [report.code, report.name]),
    ),
  );

  async open(id: string): Promise<void> {
    const request = ++this.openNumber;
    this.cancelTimers();
    this.loading.set(true);
    this.loadError.set(null);

    try {
      const [draft, vocabulary, instruments, signals] = await Promise.all([
        this.api.get(id),
        this.api.vocabulary(),
        this.api.instruments(),
        this.api.signals(),
      ]);
      if (request !== this.openNumber) return;

      this.currentDraft.set(draft);
      this.currentVocabulary.set(vocabulary);
      this.currentInstruments.set(instruments);
      this.currentSignals.set(signals);
      this.rulesDirty = false;
      this.layoutDirty = false;
      this.saveState.set('saved');
    } catch (error) {
      if (request === this.openNumber) this.loadError.set(error);
    } finally {
      if (request === this.openNumber) this.loading.set(false);
    }
  }

  async refreshInstruments(): Promise<void> {
    try {
      this.currentInstruments.set(await this.api.instruments());
    } catch (error) {
      this.reportFailure('Не вдалося оновити список фондів', error);
    }
  }

  changeGraph(graph: GraphDto): void {
    this.changeDraft((draft) => ({ ...draft, graph }));
  }

  changeSetup(setup: DraftSetupDto): void {
    this.changeDraft((draft) => ({ ...draft, setup }));
  }

  changeReports(reports: DraftReportDto[]): void {
    this.changeDraft((draft) => ({ ...draft, reports }));
  }

  rename(name: string): void {
    const trimmed = name.trim();
    if (!trimmed || trimmed === this.currentDraft()?.name) return;

    this.nameError.set(null);
    this.changeDraft((draft) => ({ ...draft, name: trimmed }));
  }

  changeDescription(description: string | null): void {
    this.changeDraft((draft) => ({ ...draft, description }));
  }

  moveNodes(layout: LayoutDto): void {
    if (this.discarding) return;
    const draft = this.currentDraft();
    if (!draft) return;

    this.currentDraft.set({ ...draft, layout });
    this.layoutDirty = true;
    this.saveState.set('pending');
    this.scheduleLayoutSave();
  }

  select(nodeId: string | null): void {
    this.currentSelection.set(nodeId);
  }

  async setMode(mode: EditorMode): Promise<void> {
    if (mode === this.currentMode()) return;

    await this.flush();
    this.currentMode.set(mode);
  }

  async flush(): Promise<void> {
    this.cancelTimers();
    await this.saveRules();
    if (!this.rulesDirty && !this.saveConflict()) await this.saveLayout();
  }

  async discard(): Promise<boolean> {
    const draft = this.currentDraft();
    if (!draft || this.busy()) return false;
    this.busy.set(true);
    this.discarding = true;
    this.cancelTimers();
    try {
      // Let already submitted saves settle before deleting, then suppress queued saves.
      await Promise.allSettled([this.rulesSave, this.layoutSave]);
      this.cancelTimers();
      await this.api.discard(draft.id);
      this.rulesDirty = false;
      this.layoutDirty = false;
      this.currentDraft.set(null);
      this.messages.add({ severity: 'success', summary: 'Чернетку видалено' });
      return true;
    } catch (error) {
      this.reportFailure('Не вдалося видалити чернетку', error);
      return false;
    } finally {
      this.discarding = false;
      this.busy.set(false);
      if (this.currentDraft()) {
        if (this.rulesDirty) this.scheduleRulesSave();
        if (this.layoutDirty) this.scheduleLayoutSave();
      }
    }
  }

  async check(): Promise<boolean> {
    await this.flush();
    const draft = this.currentDraft();
    if (!draft || this.rulesDirty || this.layoutDirty) return false;

    try {
      const check = await this.api.check(draft.id);
      this.reviewVisible.set(!check.isClean);
      this.currentDraft.update((current) =>
        current ? { ...current, problems: check.problems } : current,
      );

      return check.isClean;
    } catch (error) {
      this.reportFailure('Не вдалося перевірити чернетку', error);

      return false;
    }
  }

  async runTrial(): Promise<TrialRunDto | null> {
    if (this.busy()) return null;
    this.busy.set(true);

    try {
      if (!(await this.check())) return null;
      const draft = this.currentDraft();

      return draft ? await this.api.runTrial(draft.id) : null;
    } catch (error) {
      this.placeProblems(error);
      this.reportFailure('Не вдалося запустити бектест', error);

      return null;
    } finally {
      this.busy.set(false);
    }
  }

  async publish(): Promise<PublishedVersionDto | null> {
    if (this.busy()) return null;
    this.busy.set(true);
    this.nameError.set(null);

    try {
      if (!(await this.check())) return null;
      const draft = this.currentDraft();
      if (!draft) return null;

      return await this.api.publish(draft.id, draft.revision);
    } catch (error) {
      this.placeProblems(error);
      if (problemCode(error) === 'strategy.name_taken') this.nameError.set(toProblemMessage(error));
      this.reportFailure('Не вдалося опублікувати стратегію', error);

      return null;
    } finally {
      this.busy.set(false);
    }
  }

  private placeProblems(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) return;
    const problems = error.error?.parameters?.problems as Partial<DraftProblemDto>[] | undefined;
    if (!Array.isArray(problems)) return;

    const translated = translateDeep(problems) as Partial<DraftProblemDto>[];
    const placed = translated.map((problem) => ({
      message: problem.message ?? '',
      severity: problem.severity ?? 'Error',
      nodeId: problem.nodeId ?? null,
      block: problem.block ?? null,
      reportCode: problem.reportCode ?? null,
    }));
    this.reviewVisible.set(true);
    this.currentDraft.update((draft) => (draft ? { ...draft, problems: placed } : draft));
  }

  async copyEntryToExit(margin: number): Promise<boolean> {
    await this.flush();
    const draft = this.currentDraft();
    if (!draft || this.rulesDirty || this.layoutDirty || this.saveConflict()) return false;

    try {
      const copied = await this.api.copyEntryToExit(draft.id, draft.revision, margin);
      this.currentDraft.set(copied);
      this.currentSelection.set(null);
      this.messages.add({ severity: 'success', summary: 'Правила виходу скопійовано' });

      return true;
    } catch (error) {
      this.reportFailure('Не вдалося скопіювати правила виходу', error);

      return false;
    }
  }

  async rules(): Promise<DraftRulesDto | null> {
    const draft = this.currentDraft();
    if (!draft) return null;

    await this.flush();
    if (this.rulesDirty || this.layoutDirty || this.saveConflict()) return null;

    try {
      return await this.api.rules(draft.id);
    } catch (error) {
      this.reportFailure('Не вдалося згенерувати JSON', error);

      return null;
    }
  }

  private changeDraft(change: (draft: DraftDto) => DraftDto): void {
    if (this.discarding) return;
    const draft = this.currentDraft();
    if (!draft) return;

    if (!this.saveConflict()) this.saveError.set(null);
    this.currentDraft.set(change(draft));
    this.changeNumber++;
    this.rulesDirty = true;
    this.saveState.set('pending');
    this.scheduleRulesSave();
  }

  private scheduleRulesSave(): void {
    if (this.rulesTimer) clearTimeout(this.rulesTimer);
    this.rulesTimer = setTimeout(() => void this.saveRules(), RULES_SAVE_DELAY_MS);
  }

  private scheduleLayoutSave(): void {
    if (this.layoutTimer) clearTimeout(this.layoutTimer);
    this.layoutTimer = setTimeout(() => void this.saveLayout(), LAYOUT_SAVE_DELAY_MS);
  }

  private async saveRules(): Promise<void> {
    if (this.discarding) return;
    if (this.saveConflict()) return;
    if (!this.rulesDirty) {
      if (this.rulesSave) await this.rulesSave;
      this.updateSaveState();

      return;
    }
    if (this.rulesSave) {
      await this.rulesSave;
      if (this.rulesDirty) await this.saveRules();

      return;
    }

    let failed = false;
    this.rulesSave = this.performRulesSave().then((succeeded) => {
      failed = !succeeded;
    });
    await this.rulesSave;
    this.rulesSave = null;
    this.updateSaveState();

    if (this.rulesDirty && !failed) await this.saveRules();
  }

  private async performRulesSave(): Promise<boolean> {
    const draft = this.currentDraft();
    if (!draft) return true;

    const savedChangeNumber = this.changeNumber;
    this.rulesDirty = false;
    this.saveState.set('saving');

    try {
      const saved = await this.api.change(draft.id, {
        revision: draft.revision,
        name: draft.name,
        description: draft.description,
        setup: draft.setup,
        graph: draft.graph,
        reports: draft.reports,
      });
      this.saveError.set(null);
      const current = this.currentDraft();
      if (!current || current.id !== saved.id) return true;

      if (savedChangeNumber === this.changeNumber) {
        this.currentDraft.set({ ...saved, layout: current.layout });
      } else {
        this.currentDraft.set({
          ...current,
          revision: saved.revision,
          updatedAt: saved.updatedAt,
          problems: saved.problems,
          words: saved.words,
        });
      }
    } catch (error) {
      if (problemCode(error) === 'builder.draft.stale') {
        this.rulesDirty = true;
        this.saveConflict.set(true);
        this.saveError.set(
          'Чернетку змінили в іншому вікні. Ваші зміни залишилися тут. Збережіть локальну копію перед відкриттям серверної версії.',
        );
        return false;
      } else {
        this.rulesDirty = true;
        this.saveError.set('Не вдалося зберегти зміни. Перевірте з’єднання та спробуйте ще раз.');
        this.placeProblems(error);
        this.reportFailure('Чернетку не збережено', error);
        return false;
      }
    } finally {
      this.updateSaveState();
    }

    return true;
  }

  private async saveLayout(): Promise<void> {
    if (this.discarding) return;
    if (this.saveConflict()) return;
    if (!this.layoutDirty) {
      if (this.layoutSave) await this.layoutSave;
      this.updateSaveState();

      return;
    }
    if (this.layoutSave) {
      await this.layoutSave;
      if (this.layoutDirty) await this.saveLayout();

      return;
    }

    let failed = false;
    this.layoutSave = this.performLayoutSave().then((succeeded) => {
      failed = !succeeded;
    });
    await this.layoutSave;
    this.layoutSave = null;
    this.updateSaveState();

    if (this.layoutDirty && !failed) await this.saveLayout();
  }

  private async performLayoutSave(): Promise<boolean> {
    const draft = this.currentDraft();
    if (!draft) return true;

    this.layoutDirty = false;
    this.saveState.set('saving');

    try {
      await this.api.moveNodes(draft.id, draft.layout);
      this.saveError.set(null);
    } catch (error) {
      this.layoutDirty = true;
      this.saveError.set('Не вдалося зберегти розташування схеми. Спробуйте ще раз.');
      this.reportFailure('Розташування схеми не збережено', error);
      return false;
    } finally {
      this.updateSaveState();
    }

    return true;
  }

  downloadLocalCopy(): void {
    const draft = this.currentDraft();
    if (!draft) return;
    const url = URL.createObjectURL(
      new Blob([JSON.stringify(draft, null, 2)], { type: 'application/json' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = `${draft.name}-local-draft.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  private updateSaveState(): void {
    const saving = this.rulesSave !== null || this.layoutSave !== null;
    const pending = this.rulesDirty || this.layoutDirty;
    this.saveState.set(saving ? 'saving' : pending ? 'pending' : 'saved');
  }

  private cancelTimers(): void {
    if (this.rulesTimer) clearTimeout(this.rulesTimer);
    if (this.layoutTimer) clearTimeout(this.layoutTimer);
    this.rulesTimer = null;
    this.layoutTimer = null;
  }

  private reportFailure(summary: string, error: unknown): void {
    this.messages.add({
      severity: 'error',
      summary,
      detail: toProblemMessage(error),
      life: 10000,
    });
  }
}
