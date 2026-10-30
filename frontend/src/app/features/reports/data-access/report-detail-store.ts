import { computed, DestroyRef, effect, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { OpenActionsBadge } from '../../../core/notifications/open-actions-badge';
import { FillInput, ReportActionVm } from '../models/report-vm';
import { ReportsApi } from './reports-api';
import { summarizeProgress, toReportVm } from './report-mapper';

/**
 * One report as a working document. Marks go to the API at once; the journal answers asynchronously (outbox), so
 * after a command the report is refreshed a few times until every fill is confirmed or taken back.
 */
@Injectable()
export class ReportDetailStore {
  private readonly api = inject(ReportsApi);
  private readonly messages = inject(MessageService);
  private readonly badge = inject(OpenActionsBadge);
  private readonly accounts = inject(AccountContext);

  private readonly reportId = signal<string | undefined>(undefined);
  private readonly busyActionId = signal<string | undefined>(undefined);
  private readonly busyReport = signal<'close' | 'regenerate' | 'quotes' | null>(null);
  private refreshTimers: ReturnType<typeof setTimeout>[] = [];

  private readonly reportResource = resource({
    params: () => this.reportId(),
    loader: async ({ params: id }) => toReportVm(await this.api.get(id)),
  });

  readonly report = computed(() => (this.reportResource.hasValue() ? this.reportResource.value() : undefined));
  readonly isLoading = this.reportResource.isLoading;
  readonly loadError = this.reportResource.error;
  readonly actions = computed(() => this.report()?.actions ?? []);
  readonly progress = computed(() => summarizeProgress(this.actions()));
  readonly canMark = computed(() => this.report()?.isOpenForMarking ?? false);
  readonly busyAction = this.busyActionId.asReadonly();
  readonly busy = this.busyReport.asReadonly();

  constructor() {
    inject(DestroyRef).onDestroy(() => this.refreshTimers.forEach(clearTimeout));
    // Opened from a link (a notification, another account's list): the top bar follows the report's account.
    effect(() => {
      const accountId = this.report()?.summary.accountId;
      if (accountId && accountId !== this.accounts.currentId()) this.accounts.focus(accountId);
    });
  }

  open(reportId: string): void {
    this.reportId.set(reportId);
  }

  markExecuted(action: ReportActionVm, fill: FillInput): Promise<void> {
    return this.runCommand(action, () => this.api.markExecuted(this.requireId(), action.id, fill), 'Відмічено як виконане');
  }

  recordPartialFill(action: ReportActionVm, fill: FillInput): Promise<void> {
    return this.runCommand(action, () => this.api.recordPartialFill(this.requireId(), action.id, fill), 'Часткове виконання збережено');
  }

  skip(action: ReportActionVm, reason: string): Promise<void> {
    return this.runCommand(action, () => this.api.skip(this.requireId(), action.id, reason), 'Дію пропущено');
  }

  undo(action: ReportActionVm): Promise<void> {
    return this.runCommand(action, () => this.api.undo(this.requireId(), action.id), 'Відмітку скасовано, операцію в журналі сторновано');
  }

  /** After actions were marked from the journal elsewhere (the "load operations" dialog). */
  marked(count: number): void {
    this.messages.add({ severity: 'success', summary: 'Операції підтягнуто', detail: `Відмічено дій: ${count}` });
    this.afterChange();
  }

  async close(): Promise<void> {
    this.busyReport.set('close');
    try {
      await this.api.close(this.requireId());
      this.messages.add({ severity: 'success', summary: 'Рапорт закрито', detail: 'Невідмічені дії позначено як пропущені' });
      this.afterChange();
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося закрити', detail: toProblemMessage(error) });
    } finally {
      this.busyReport.set(null);
    }
  }

  /** "Generate again": a new plan from what is really held now; this report is superseded. */
  async regenerate(): Promise<string | undefined> {
    const report = this.report();
    if (!report) return undefined;
    this.busyReport.set('regenerate');
    try {
      const accountId = report.summary.accountId;
      const { reportId } = await this.api.generate(accountId, report.summary.code);
      this.messages.add({ severity: 'success', summary: 'Новий рапорт готовий', detail: 'Попередній замінено' });
      this.badge.refresh();
      return reportId;
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося згенерувати', detail: toProblemMessage(error), life: 8000 });
      return undefined;
    } finally {
      this.busyReport.set(null);
    }
  }

  async refreshQuotes(): Promise<void> {
    this.busyReport.set('quotes');
    try {
      await this.api.ingestQuotes();
      this.messages.add({ severity: 'success', summary: 'Котирування оновлено', detail: 'Згенеруйте рапорт заново, щоб план врахував нові ціни' });
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося оновити котирування', detail: toProblemMessage(error) });
    } finally {
      this.busyReport.set(null);
    }
  }

  private async runCommand(action: ReportActionVm, command: () => Promise<unknown>, success: string): Promise<void> {
    this.busyActionId.set(action.id);
    try {
      await command();
      this.messages.add({ severity: 'success', summary: success, detail: action.title });
      this.afterChange();
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося зберегти', detail: toProblemMessage(error), life: 8000 });
    } finally {
      this.busyActionId.set(undefined);
    }
  }

  private afterChange(): void {
    this.reportResource.reload();
    this.badge.refresh();
    this.refreshTimers.forEach(clearTimeout);
    this.refreshTimers = [1500, 4000].map((delay) => setTimeout(() => this.reportResource.reload(), delay));
  }

  private requireId(): string {
    const id = this.reportId();
    if (!id) throw new Error('Report is not opened');
    return id;
  }
}
