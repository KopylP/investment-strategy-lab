import { translate } from '@jsverse/transloco';
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DeclarationDto, DeclarationForm, Pit8CAmounts } from '../models/declaration-dto';
import {
  BrokerStatement,
  BrokerStatements,
  LossCarryforward,
  RecordLossBody,
  TaxOverview,
  TaxYearDetails,
  UploadBrokerStatementBody,
} from '../models/tax-dto';

const PDF_LINK_LIFETIME_MS = 10_000;

@Injectable()
export class TaxesStore {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);
  private readonly accounts = inject(AccountContext);

  readonly selectedYear = signal<number | null>(null);

  private readonly yearsResource = resource({
    params: () => (this.accounts.current() ? true : undefined),
    loader: () => this.api.get<TaxOverview>('/api/taxation/years'),
  });

  private readonly detailsResource = resource({
    params: () => this.year() ?? undefined,
    loader: ({ params: year }) => this.api.get<TaxYearDetails>(`/api/taxation/years/${year}`),
  });

  private readonly brokerResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) =>
      this.api.get<BrokerStatements>(`/api/taxation/accounts/${accountId}/broker-statements`),
  });

  private readonly lossesResource = resource({
    params: () => (this.accounts.current() ? true : undefined),
    loader: () => this.api.get<LossCarryforward[]>('/api/taxation/losses'),
  });

  private readonly formsResource = resource({
    params: () => this.year() ?? undefined,
    loader: ({ params: year }) =>
      this.api.get<DeclarationForm[]>(`/api/taxation/declarations/${year}`),
  });

  readonly declarationCode = signal<string | null>(null);

  private readonly declarationResource = resource({
    params: () =>
      this.declarationCode() && this.year()
        ? { code: this.declarationCode()!, year: this.year()! }
        : undefined,
    loader: ({ params }) =>
      this.api.get<DeclarationDto>(
        `/api/taxation/declarations/${params.year}/${encodeURIComponent(params.code)}`,
      ),
  });

  readonly forms = computed(() => this.formsResource.value() ?? []);
  readonly declaration = computed(() =>
    this.declarationResource.hasValue() ? this.declarationResource.value() : undefined,
  );
  readonly declarationLoading = this.declarationResource.isLoading;
  readonly declarationError = this.declarationResource.error;
  readonly declarationProblem = computed(() => toProblemMessage(this.declarationResource.error()));

  async setPit8C(accountId: string, amounts: Pit8CAmounts): Promise<boolean> {
    return this.change(
      () => this.api.put(`/api/taxation/declarations/${this.year()}/pit-8c/${accountId}`, amounts),
      translate('taxes.pit8cSaved'),
    );
  }

  async removePit8C(accountId: string): Promise<boolean> {
    return this.change(
      () => this.api.delete(`/api/taxation/declarations/${this.year()}/pit-8c/${accountId}`),
      translate('taxes.pit8cRemoved'),
    );
  }

  readonly pdfLoading = signal<'summary' | 'form' | null>(null);

  async downloadDeclarationPdf(kind: 'summary' | 'form'): Promise<void> {
    const code = this.declarationCode();
    const year = this.year();
    if (!code || !year) return;

    this.pdfLoading.set(kind);

    try {
      const pdf = await this.api.blob(
        `/api/taxation/declarations/${year}/${encodeURIComponent(code)}/pdf?kind=${kind}`,
      );
      savePdf(pdf, `${code}-${year}-${kind === 'form' ? 'formularz' : 'sciaga'}.pdf`);
    } catch (error) {
      const problem = await readableProblem(error);

      this.messages.add({
        severity: 'error',
        summary: translate('taxes.pdfFailed'),
        detail: toProblemMessage(problem),
      });
    } finally {
      this.pdfLoading.set(null);
    }
  }

  readonly account = this.accounts.current;
  readonly allAccounts = this.accounts.accounts;
  readonly years = computed(() =>
    this.yearsResource.hasValue() ? (this.yearsResource.value()?.years ?? []) : [],
  );
  readonly taxAccounts = computed(() =>
    this.yearsResource.hasValue() ? (this.yearsResource.value()?.accounts ?? []) : [],
  );
  readonly taxedAccounts = computed(() => this.taxAccounts().filter((account) => account.isTaxed));
  readonly untaxedAccounts = computed(() =>
    this.taxAccounts().filter((account) => !account.isTaxed),
  );
  readonly showAccount = computed(() => this.taxedAccounts().length > 1);
  readonly isLoading = computed(
    () => this.yearsResource.isLoading() || this.detailsResource.isLoading(),
  );
  readonly loadError = this.yearsResource.error;
  readonly year = computed(() => this.selectedYear() ?? this.years()[0]?.year ?? null);
  readonly details = computed(() =>
    this.detailsResource.hasValue() ? this.detailsResource.value() : undefined,
  );
  readonly losses = computed(() => this.lossesResource.value() ?? []);
  readonly broker = computed(() => this.brokerResource.value());
  readonly isTaxed = computed(() => this.taxedAccounts().length > 0);
  readonly saving = signal(false);

  symbol(id: string): string {
    return this.details()?.instruments[id] ?? id.slice(0, 8);
  }

  accountName(id: string): string {
    return this.taxAccounts().find((account) => account.id === id)?.name ?? '';
  }

  chooseAccount(id: string): void {
    this.accounts.choose(id);
  }

  async addLoss(year: number, amount: number, alreadyUsed: number): Promise<boolean> {
    return this.change(
      () => {
        const body: RecordLossBody = { year, amount, alreadyUsed };

        return this.api.post<LossCarryforward>('/api/taxation/losses', body);
      },
      translate('taxes.lossAdded'),
    );
  }

  async removeLoss(loss: LossCarryforward): Promise<boolean> {
    return this.change(
      () => this.api.delete(`/api/taxation/losses/${loss.id}`),
      translate('taxes.lossRemoved'),
    );
  }

  async uploadBrokerStatement(file: File): Promise<boolean> {
    const bytes = new Uint8Array(await file.arrayBuffer());
    let binary = '';

    for (let i = 0; i < bytes.length; i += 0x8000)
      binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));

    const body: UploadBrokerStatementBody = { fileName: file.name, contentBase64: btoa(binary) };

    return this.change(
      () =>
        this.api.post<BrokerStatements>(
          `/api/taxation/accounts/${this.requireAccount()}/broker-statements`,
          body,
        ),
      translate('taxes.statementApplied'),
    );
  }

  async removeBrokerStatement(statement: BrokerStatement): Promise<boolean> {
    return this.change(
      () =>
        this.api.delete(
          `/api/taxation/accounts/${this.requireAccount()}/broker-statements/${statement.id}`,
        ),
      translate('taxes.statementRemoved'),
    );
  }

  private async change(command: () => Promise<unknown>, success: string): Promise<boolean> {
    this.saving.set(true);

    try {
      await command();
      this.messages.add({ severity: 'success', summary: success });
      this.reloadEverything();

      return true;
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: translate('profile.notSaved'),
        detail: toProblemMessage(error),
      });

      return false;
    } finally {
      this.saving.set(false);
    }
  }

  private reloadEverything(): void {
    this.lossesResource.reload();
    this.brokerResource.reload();
    this.yearsResource.reload();
    this.detailsResource.reload();
    this.formsResource.reload();
    this.declarationResource.reload();
  }

  private requireAccount(): string {
    const id = this.accounts.currentId();
    if (!id) throw new Error('No account');

    return id;
  }
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return { detail: text };
  }
}

function savePdf(pdf: Blob, fileName: string): void {
  const link = document.createElement('a');
  link.href = URL.createObjectURL(pdf);
  link.download = fileName;
  link.click();

  setTimeout(() => URL.revokeObjectURL(link.href), PDF_LINK_LIFETIME_MS);
}

async function readableProblem(error: unknown): Promise<unknown> {
  const isBlobError = error instanceof HttpErrorResponse && error.error instanceof Blob;
  const body = isBlobError ? await error.error.text().catch(() => '') : '';

  if (!body) return error;

  return new HttpErrorResponse({
    error: safeJson(body),
    status: (error as HttpErrorResponse).status,
  });
}
