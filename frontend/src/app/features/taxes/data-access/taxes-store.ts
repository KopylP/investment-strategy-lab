import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DeclarationForm, Pit8CAmounts, TaxDeclaration } from './declarations';

export interface TaxYearSummary {
  year: number;
  revenue: number;
  costs: number;
  gain: number;
  carryforwardUsed: number;
  taxBase: number;
  capitalTax: number;
  dividendIncome: number;
  dividendTax: number;
  taxDue: number;
  lossToCarry: number;
  dividendTaxCredit: number;
}

export interface Realization {
  saleTransactionId: string;
  soldOn: string;
  instrumentId: string;
  quantity: number;
  proceedsInTaxCurrency: number;
  costInTaxCurrency: number;
  gainInTaxCurrency: number;
  fxRate: number;
  fxRateDate: string | null;
  lots: {
    buyTransactionId: string;
    acquiredOn: string;
    quantity: number;
    costInTaxCurrency: number;
    fromBroker: boolean;
  }[];
  unmatchedQuantity: number;
  brokerGainInTaxCurrency: number | null;
  accountId: string;
}

export interface TaxAccount {
  id: string;
  name: string;
  broker: string;
  currency: string;
  policyCode: string;
  isTaxed: boolean;
}

export interface TaxOverview {
  years: { year: number; policyCode: string; computedAt: string; summary: TaxYearSummary }[];
  accounts: TaxAccount[];
}

export interface DividendIncome {
  transactionId: string;
  receivedOn: string;
  instrumentId: string;
  amount: number;
  currency: string | null;
  amountInTaxCurrency: number;
  fxRate: number;
  grossInTaxCurrency: number;
  withheldInTaxCurrency: number;
  accountId: string;
}

export interface BrokerStatement {
  id: string;
  broker: string;
  fileName: string;
  uploadedAt: string;
  from: string;
  to: string;
  sales: number;
  dividends: number;
  warnings: string[];
}

export interface BrokerStatements {
  statements: BrokerStatement[];
  sales: number;
  matchedSales: number;
  dividends: number;
  matchedDividends: number;
}

export interface TaxYearDetails {
  year: number;
  policyCode: string;
  fxRule: string;
  isTaxed: boolean;
  rateLabel: string;
  computedAt: string;
  summary: TaxYearSummary;
  realizations: Realization[];
  dividends: DividendIncome[];
  carryforwardUses: { lossYear: number; amount: number }[];
  openLots: {
    buyTransactionId: string;
    acquiredOn: string;
    instrumentId: string;
    quantity: number;
    costInTaxCurrency: number;
    fxRate: number;
    accountId: string;
  }[];
  instruments: Record<string, string>;
  accounts: TaxAccount[];
  warnings: string[];
}

export interface LossCarryforward {
  id: string;
  year: number;
  amount: number;
  alreadyUsed: number;
}

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
      this.api.get<{ declaration: TaxDeclaration; warnings: string[] }>(
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
      'Суми PIT-8C збережено',
    );
  }

  async removePit8C(accountId: string): Promise<boolean> {
    return this.change(
      () => this.api.delete(`/api/taxation/declarations/${this.year()}/pit-8c/${accountId}`),
      'Суми PIT-8C видалено',
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
        summary: 'PDF не створено',
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
      () => this.api.post('/api/taxation/losses', { year, amount, alreadyUsed }),
      'Збиток додано',
    );
  }

  async removeLoss(loss: LossCarryforward): Promise<boolean> {
    return this.change(() => this.api.delete(`/api/taxation/losses/${loss.id}`), 'Збиток видалено');
  }

  async uploadBrokerStatement(file: File): Promise<boolean> {
    const bytes = new Uint8Array(await file.arrayBuffer());
    let binary = '';

    for (let i = 0; i < bytes.length; i += 0x8000)
      binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));

    return this.change(
      () =>
        this.api.post(`/api/taxation/accounts/${this.requireAccount()}/broker-statements`, {
          fileName: file.name,
          contentBase64: btoa(binary),
        }),
      'Звіт брокера враховано',
    );
  }

  async removeBrokerStatement(statement: BrokerStatement): Promise<boolean> {
    return this.change(
      () =>
        this.api.delete(
          `/api/taxation/accounts/${this.requireAccount()}/broker-statements/${statement.id}`,
        ),
      'Звіт брокера видалено',
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
        summary: 'Не збережено',
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
