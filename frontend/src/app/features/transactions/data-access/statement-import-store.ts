import { computed, effect, inject, Injectable, resource, signal, untracked } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';

export type ImportMode = 'Merge' | 'UpdateImported' | 'Replace' | 'FromScratch';
export type RowDecision =
  | 'New'
  | 'AlreadyImported'
  | 'Differs'
  | 'MatchesExisting'
  | 'OutOfScope'
  | 'Unsupported'
  | 'NeedsMapping';

export interface TickerMapping {
  ticker: string;
  currency: string;
  instrumentId: string | null;
  registerAs: string | null;
  subPortfolioId: string | null;
  rows: number;
  instrumentName: string | null;
}

export interface StatementRow {
  number: number;
  occurredOn: string;
  kind: string;
  rawType: string;
  ticker: string | null;
  quantity: number | null;
  price: number | null;
  amount: number;
  currency: string;
  amountInAccountCurrency: number;
  decision: RowDecision;
  note: string | null;
  willRecord: boolean;
  willReverseExisting: boolean;
  result: string | null;
}

export interface StatementImport {
  id: string;
  broker: string;
  fileName: string;
  uploadedAt: string;
  status: 'Preview' | 'Confirmed' | 'Undone';
  mode: ImportMode;
  fromDate: string | null;
  cashSubPortfolioId: string | null;
  accountCurrency: string;
  mappings: TickerMapping[];
  rows: StatementRow[];
  counts: {
    new: number;
    alreadyImported: number;
    differs: number;
    matchesExisting: number;
    outOfScope: number;
    unsupported: number;
    needsMapping: number;
    toRecord: number;
    toReverse: number;
  };
  warnings: string[];
  subPortfolios: { id: string; name: string }[];
  confirmedAt: string | null;
  undoneAt: string | null;
}

export interface ImportSummary {
  id: string;
  broker: string;
  fileName: string;
  uploadedAt: string;
  status: string;
  mode: string;
  recorded: number;
}

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
      const content = await file.text();
      this.importAccountId.set(accountId);
      this.current.set(
        await this.api.post<StatementImport>(
          `/api/portfolio/accounts/${accountId}/statement-imports`,
          { fileName: file.name, content },
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
    await this.run('plan', async () =>
      this.current.set(
        await this.api.put<StatementImport>(
          `/api/portfolio/accounts/${accountId}/statement-imports/${current.id}`,
          {
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
          },
        ),
      ),
    );
  }

  async assignUnmappedTo(subPortfolioId: string): Promise<void> {
    const current = this.current();
    const accountId = this.accounts.currentId();
    if (!current || !accountId) return;
    await this.run('plan', async () =>
      this.current.set(
        await this.api.put<StatementImport>(
          `/api/portfolio/accounts/${accountId}/statement-imports/${current.id}`,
          {
            mode: current.mode,
            fromDate: current.fromDate,
            cashSubPortfolioId: current.cashSubPortfolioId,
            mappings: current.mappings.map((mapping) => ({
              ticker: mapping.ticker,
              instrumentId: mapping.instrumentId,
              registerAs: mapping.registerAs,
              subPortfolioId: mapping.subPortfolioId ?? subPortfolioId,
            })),
          },
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
      const recorded = result.rows.filter((row) => row.result === 'записано').length;
      const failed = result.rows.filter((row) => row.result?.startsWith('не ')).length;
      this.messages.add({
        severity: failed ? 'warn' : 'success',
        summary: 'Імпорт завершено',
        detail: `Записано ${recorded}${failed ? `, не вдалося ${failed} — див. таблицю` : ''}`,
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
        summary: 'Імпорт скасовано',
        detail: 'Записані ним операції сторновано',
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
        summary: 'Не вдалося',
        detail: toProblemMessage(error),
        life: 10000,
      });
    } finally {
      this.busy.set(null);
    }
  }
}
