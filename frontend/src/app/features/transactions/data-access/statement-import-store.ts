import { translateCode } from '../../../core/i18n/translate';
import { translate } from '@jsverse/transloco';
import { computed, effect, inject, Injectable, resource, signal, untracked } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import {
  ConfigureImportBody,
  ImportMode,
  ImportSummary,
  StatementImport,
  TickerMapping,
  UploadStatementBody,
} from '../models/statement-import-dto';

@Injectable()
export class StatementImportStore {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);
  private readonly accounts = inject(AccountContext);

  readonly current = signal<StatementImport | null>(null);
  readonly busy = signal<'upload' | 'plan' | 'confirm' | 'undo' | null>(null);

  private readonly historyResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) =>
      this.api.get<ImportSummary[]>(`/api/portfolio/accounts/${accountId}/statement-imports`),
  });

  readonly account = this.accounts.current;
  readonly accountOptions = this.accounts.accounts;
  private readonly importAccountId = signal<string | undefined>(undefined);
  readonly history = computed(() => this.historyResource.value() ?? []);
  readonly unmappedCount = computed(
    () => this.current()?.mappings.filter((mapping) => !mapping.subPortfolioId).length ?? 0,
  );
  readonly canConfirm = computed(() => {
    const current = this.current();

    return (
      !!current &&
      current.status === 'Preview' &&
      current.counts.needsMapping === 0 &&
      (current.counts.toRecord > 0 || current.counts.toReverse > 0)
    );
  });

  constructor() {
    effect(() => {
      const accountId = this.accounts.currentId();
      if (untracked(this.current) && untracked(this.importAccountId) !== accountId)
        this.current.set(null);
    });
  }

  chooseAccount(accountId: string): void {
    this.accounts.choose(accountId);
  }

  async upload(file: File): Promise<void> {
    const accountId = this.accounts.currentId();
    if (!accountId) return;

    await this.run('upload', async () => {
      const body: UploadStatementBody = { fileName: file.name, content: await file.text() };

      this.importAccountId.set(accountId);
      this.current.set(
        await this.api.post<StatementImport>(
          `/api/portfolio/accounts/${accountId}/statement-imports`,
          body,
        ),
      );
    });
  }

  async open(importId: string): Promise<void> {
    const accountId = this.accounts.currentId();
    if (!accountId) return;

    this.importAccountId.set(accountId);
    await this.run('plan', async () =>
      this.current.set(
        await this.api.get<StatementImport>(
          `/api/portfolio/accounts/${accountId}/statement-imports/${importId}`,
        ),
      ),
    );
  }

  async configure(change: {
    mode?: ImportMode;
    fromDate?: string | null;
    cashSubPortfolioId?: string | null;
    mapping?: Partial<TickerMapping> & { ticker: string };
  }): Promise<void> {
    const current = this.current();
    const accountId = this.accounts.currentId();
    if (!current || !accountId) return;

    const mappings = current.mappings.map((mapping) =>
      change.mapping && mapping.ticker === change.mapping.ticker
        ? { ...mapping, ...change.mapping }
        : mapping,
    );
    const body: ConfigureImportBody = {
      mode: change.mode ?? current.mode,
      fromDate: change.fromDate !== undefined ? change.fromDate : current.fromDate,
      cashSubPortfolioId:
        change.cashSubPortfolioId !== undefined
          ? change.cashSubPortfolioId
          : current.cashSubPortfolioId,
      mappings: mappings.map((mapping) => ({
        ticker: mapping.ticker,
        instrumentId: mapping.instrumentId,
        registerAs: mapping.registerAs,
        subPortfolioId: mapping.subPortfolioId,
      })),
    };

    await this.run('plan', async () =>
      this.current.set(
        await this.api.put<StatementImport>(
          `/api/portfolio/accounts/${accountId}/statement-imports/${current.id}`,
          body,
        ),
      ),
    );
  }

  async assignUnmappedTo(subPortfolioId: string): Promise<void> {
    const current = this.current();
    const accountId = this.accounts.currentId();
    if (!current || !accountId) return;

    const body: ConfigureImportBody = {
      mode: current.mode,
      fromDate: current.fromDate,
      cashSubPortfolioId: current.cashSubPortfolioId,
      mappings: current.mappings.map((mapping) => ({
        ticker: mapping.ticker,
        instrumentId: mapping.instrumentId,
        registerAs: mapping.registerAs,
        subPortfolioId: mapping.subPortfolioId ?? subPortfolioId,
      })),
    };

    await this.run('plan', async () =>
      this.current.set(
        await this.api.put<StatementImport>(
          `/api/portfolio/accounts/${accountId}/statement-imports/${current.id}`,
          body,
        ),
      ),
    );
  }

  async confirm(): Promise<void> {
    const current = this.current();
    const accountId = this.accounts.currentId();
    if (!current || !accountId) return;

    await this.run('confirm', async () => {
      const result = await this.api.post<StatementImport>(
        `/api/portfolio/accounts/${accountId}/statement-imports/${current.id}/confirm`,
      );

      this.current.set(result);
      const recordedText = translateCode('statement.row.recorded');
      const notRecordedText = translateCode('statement.row.not_recorded', { error: '' });
      const recorded = result.rows.filter((row) => row.result === recordedText).length;
      const failed = result.rows.filter((row) => row.result?.startsWith(notRecordedText)).length;

      this.messages.add({
        severity: failed ? 'warn' : 'success',
        summary: translate('transactions.import.done'),
        detail: failed
          ? translate('transactions.import.recordedAndFailed', { recorded, failed })
          : translate('transactions.import.recordedCount', { recorded }),
        life: 8000,
      });
      this.historyResource.reload();
    });
  }

  async undo(importId: string): Promise<void> {
    const accountId = this.accounts.currentId();
    if (!accountId) return;

    await this.run('undo', async () => {
      const result = await this.api.post<StatementImport>(
        `/api/portfolio/accounts/${accountId}/statement-imports/${importId}/undo`,
      );
      if (this.current()?.id === importId) this.current.set(result);

      this.messages.add({
        severity: 'success',
        summary: translate('transactions.import.cancelled'),
        detail: translate('transactions.import.cancelledHint'),
      });
      this.historyResource.reload();
    });
  }

  reset(): void {
    this.current.set(null);
  }

  private async run(
    kind: 'upload' | 'plan' | 'confirm' | 'undo',
    work: () => Promise<void>,
  ): Promise<void> {
    this.busy.set(kind);

    try {
      await work();
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: translate('transactions.import.failed'),
        detail: toProblemMessage(error),
        life: 10000,
      });
    } finally {
      this.busy.set(null);
    }
  }
}
