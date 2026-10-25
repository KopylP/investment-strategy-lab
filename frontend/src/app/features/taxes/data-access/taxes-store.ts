import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';

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
  lots: { buyTransactionId: string; acquiredOn: string; quantity: number; costPln: number }[];
  unmatchedQuantity: number;
}

export interface TaxYearDetails {
  year: number;
  policyCode: string;
  fxRule: string;
  computedAt: string;
  summary: TaxYearSummary;
  realizations: Realization[];
  dividends: { transactionId: string; receivedOn: string; instrumentId: string; amount: number; amountPln: number; fxRate: number }[];
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

  private readonly lossesResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.get<LossCarryforward[]>(`/api/taxation/accounts/${accountId}/losses`),
  });

  readonly account = this.accounts.current;
  readonly years = computed(() => this.yearsResource.value() ?? []);
  readonly isLoading = computed(() => this.yearsResource.isLoading() || this.detailsResource.isLoading());
  readonly loadError = this.yearsResource.error;
  readonly year = computed(() => this.selectedYear() ?? this.years()[0]?.year ?? null);
  readonly details = computed(() => (this.detailsResource.hasValue() ? this.detailsResource.value() : undefined));
  readonly losses = computed(() => this.lossesResource.value() ?? []);
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

  private async change(command: () => Promise<unknown>, success: string): Promise<boolean> {
    this.saving.set(true);
    try {
      await command();
      this.messages.add({ severity: 'success', summary: success });
      this.lossesResource.reload();
      this.yearsResource.reload();
      this.detailsResource.reload();
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
