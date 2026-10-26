import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DeclarationForm, TaxDeclaration } from './declarations';

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
  /** Tax paid abroad on the year's dividends that reduces the Polish tax. */
  dividendTaxCredit: number;
}

export interface Realization {
  saleTransactionId: string;
  soldOn: string;
  instrumentId: string;
  quantity: number;
  proceedsPln: number;
  costPln: number;
  gainPln: number;
  fxRate: number;
  fxRateDate: string | null;
  lots: { buyTransactionId: string; acquiredOn: string; quantity: number; costPln: number; fromBroker: boolean }[];
  unmatchedQuantity: number;
  /** The broker's own gain of the sale (its report), to compare with ours. */
  brokerGainPln: number | null;
}

export interface DividendIncome {
  transactionId: string;
  receivedOn: string;
  instrumentId: string;
  /** As received (net), in {@link currency} (the dividend's own; missing in years computed before it was kept). */
  amount: number;
  currency: string | null;
  amountPln: number;
  fxRate: number;
  /** Before the tax withheld abroad; 0 in years computed before the broker reports existed. */
  grossPln: number;
  withheldPln: number;
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
  computedAt: string;
  summary: TaxYearSummary;
  realizations: Realization[];
  dividends: DividendIncome[];
  carryforwardUses: { lossYear: number; amount: number }[];
  openLots: { buyTransactionId: string; acquiredOn: string; instrumentId: string; quantity: number; costPln: number; fxRate: number }[];
  instruments: Record<string, string>;
  warnings: string[];
}

export interface LossCarryforward {
  id: string;
  year: number;
  amount: number;
  alreadyUsed: number;
}

/** Belka per year: realisations FIFO in PLN, carried losses, the tax due. Always recomputed on read. */
@Injectable()
export class TaxesStore {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);
  private readonly accounts = inject(AccountContext);

  readonly selectedYear = signal<number | null>(null);

  private readonly yearsResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.get<{ year: number; summary: TaxYearSummary; policyCode: string }[]>(`/api/taxation/accounts/${accountId}/years`),
  });

  private readonly detailsResource = resource({
    params: () => (this.accounts.currentId() && this.year() ? { accountId: this.accounts.currentId()!, year: this.year()! } : undefined),
    loader: ({ params }) => this.api.get<TaxYearDetails>(`/api/taxation/accounts/${params.accountId}/years/${params.year}`),
  });

  private readonly brokerResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.get<BrokerStatements>(`/api/taxation/accounts/${accountId}/broker-statements`),
  });

  private readonly lossesResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.get<LossCarryforward[]>(`/api/taxation/accounts/${accountId}/losses`),
  });

  /** Forms the user's accounts call for in the selected year (none for IKE-only or foreign policies yet). */
  private readonly formsResource = resource({
    params: () => this.year() ?? undefined,
    loader: ({ params: year }) => this.api.get<DeclarationForm[]>(`/api/taxation/declarations/${year}`),
  });

  readonly declarationCode = signal<string | null>(null);

  private readonly declarationResource = resource({
    params: () => (this.declarationCode() && this.year() ? { code: this.declarationCode()!, year: this.year()! } : undefined),
    loader: ({ params }) => this.api.get<{ declaration: TaxDeclaration; warnings: string[] }>(`/api/taxation/declarations/${params.year}/${encodeURIComponent(params.code)}`),
  });

  readonly forms = computed(() => this.formsResource.value() ?? []);
  readonly declaration = computed(() => (this.declarationResource.hasValue() ? this.declarationResource.value() : undefined));
  readonly declarationLoading = this.declarationResource.isLoading;
  readonly declarationError = this.declarationResource.error;

  readonly pdfLoading = signal(false);

  /** The filled return on the official form (PIT-38 with its PIT/ZG), saved as a file. */
  async downloadDeclarationPdf(): Promise<void> {
    const code = this.declarationCode();
    const year = this.year();
    if (!code || !year) return;
    this.pdfLoading.set(true);
    try {
      const pdf = await this.api.blob(`/api/taxation/declarations/${year}/${encodeURIComponent(code)}/pdf`);
      const link = document.createElement('a');
      link.href = URL.createObjectURL(pdf);
      link.download = `${code}-${year}.pdf`;
      link.click();
      setTimeout(() => URL.revokeObjectURL(link.href), 10_000);
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'PDF не створено', detail: toProblemMessage(error) });
    } finally {
      this.pdfLoading.set(false);
    }
  }

  readonly account = this.accounts.current;
  readonly years = computed(() => this.yearsResource.value() ?? []);
  readonly isLoading = computed(() => this.yearsResource.isLoading() || this.detailsResource.isLoading());
  readonly loadError = this.yearsResource.error;
  readonly year = computed(() => this.selectedYear() ?? this.years()[0]?.year ?? null);
  readonly details = computed(() => (this.detailsResource.hasValue() ? this.detailsResource.value() : undefined));
  readonly losses = computed(() => this.lossesResource.value() ?? []);
  readonly broker = computed(() => this.brokerResource.value());
  readonly isTaxed = computed(() => !this.years()[0] || this.years()[0].policyCode === 'PL-BELKA');
  readonly saving = signal(false);

  symbol(id: string): string {
    return this.details()?.instruments[id] ?? id.slice(0, 8);
  }

  async addLoss(year: number, amount: number, alreadyUsed: number): Promise<boolean> {
    return this.change(() => this.api.post(`/api/taxation/accounts/${this.requireAccount()}/losses`, { year, amount, alreadyUsed }), 'Збиток додано');
  }

  async removeLoss(loss: LossCarryforward): Promise<boolean> {
    return this.change(() => this.api.delete(`/api/taxation/accounts/${this.requireAccount()}/losses/${loss.id}`), 'Збиток видалено');
  }

  /** The broker's P&L report (PDF) goes as base64 inside JSON, like the CSV statement goes as text. */
  async uploadBrokerStatement(file: File): Promise<boolean> {
    const bytes = new Uint8Array(await file.arrayBuffer());
    let binary = '';
    for (let i = 0; i < bytes.length; i += 0x8000) binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
    return this.change(
      () => this.api.post(`/api/taxation/accounts/${this.requireAccount()}/broker-statements`, { fileName: file.name, contentBase64: btoa(binary) }),
      'Звіт брокера враховано',
    );
  }

  async removeBrokerStatement(statement: BrokerStatement): Promise<boolean> {
    return this.change(() => this.api.delete(`/api/taxation/accounts/${this.requireAccount()}/broker-statements/${statement.id}`), 'Звіт брокера видалено');
  }

  private async change(command: () => Promise<unknown>, success: string): Promise<boolean> {
    this.saving.set(true);
    try {
      await command();
      this.messages.add({ severity: 'success', summary: success });
      this.lossesResource.reload();
      this.brokerResource.reload();
      this.yearsResource.reload();
      this.detailsResource.reload();
      this.formsResource.reload();
      return true;
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не збережено', detail: toProblemMessage(error) });
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  private requireAccount(): string {
    const id = this.accounts.currentId();
    if (!id) throw new Error('No account');
    return id;
  }
}
