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
  accountId: string;
}

/** An account of the taxpayer; IKE / IKZE accounts are not taxed and stay out of PIT-38. */
export interface TaxAccount {
  id: string;
  name: string;
  policyCode: string;
  isTaxed: boolean;
}

export interface TaxYearListItem {
  year: number;
  policyCode: string;
  computedAt: string;
  summary: TaxYearSummary;
}

export interface TaxOverview {
  years: TaxYearListItem[];
  accounts: TaxAccount[];
}

export interface TaxYearDetails {
  year: number;
  policyCode: string;
  fxRule: string;
  computedAt: string;
  summary: TaxYearSummary;
  realizations: Realization[];
  dividends: { transactionId: string; receivedOn: string; instrumentId: string; amount: number; amountPln: number; fxRate: number; accountId: string }[];
  carryforwardUses: { lossYear: number; amount: number }[];
  openLots: { buyTransactionId: string; acquiredOn: string; instrumentId: string; quantity: number; costPln: number; fxRate: number; accountId: string }[];
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

/**
 * Belka per year for the signed-in taxpayer — one PIT-38 over all taxed accounts: FIFO within each account, gains and
 * losses of all brokers netted, carried losses, the tax due. Always recomputed on read; not tied to the chosen account.
 */
@Injectable()
export class TaxesStore {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);
  private readonly accounts = inject(AccountContext);

  readonly selectedYear = signal<number | null>(null);

  private readonly overviewResource = resource({
    loader: () => this.api.get<TaxOverview>('/api/taxation/years'),
  });

  private readonly detailsResource = resource({
    params: () => this.year() ?? undefined,
    loader: ({ params: year }) => this.api.get<TaxYearDetails>(`/api/taxation/years/${year}`),
  });

  private readonly lossesResource = resource({
    loader: () => this.api.get<LossCarryforward[]>('/api/taxation/losses'),
  });

  readonly hasAccounts = computed(() => this.accounts.accounts().length > 0);
  readonly years = computed(() => this.overviewResource.value()?.years ?? []);
  readonly taxAccounts = computed(() => this.overviewResource.value()?.accounts ?? []);
  readonly taxedAccounts = computed(() => this.taxAccounts().filter((account) => account.isTaxed));
  readonly untaxedAccounts = computed(() => this.taxAccounts().filter((account) => !account.isTaxed));
  /** The account column matters only when more than one account goes into PIT-38. */
  readonly showAccount = computed(() => this.taxedAccounts().length > 1);
  readonly isLoading = computed(() => this.overviewResource.isLoading() || this.detailsResource.isLoading());
  readonly loadError = this.overviewResource.error;
  readonly year = computed(() => this.selectedYear() ?? this.years()[0]?.year ?? null);
  readonly details = computed(() => (this.detailsResource.hasValue() ? this.detailsResource.value() : undefined));
  readonly losses = computed(() => this.lossesResource.value() ?? []);
  readonly saving = signal(false);

  symbol(id: string): string {
    return this.details()?.instruments[id] ?? id.slice(0, 8);
  }

  accountName(id: string): string {
    return this.taxAccounts().find((account) => account.id === id)?.name ?? '';
  }

  /** Amounts of a dividend are in its account's currency. */
  currencyOf(accountId: string): string {
    return this.accounts.accounts().find((account) => account.id === accountId)?.baseCurrency ?? 'EUR';
  }

  async addLoss(year: number, amount: number, alreadyUsed: number): Promise<boolean> {
    return this.change(() => this.api.post('/api/taxation/losses', { year, amount, alreadyUsed }), 'Збиток додано');
  }

  async removeLoss(loss: LossCarryforward): Promise<boolean> {
    return this.change(() => this.api.delete(`/api/taxation/losses/${loss.id}`), 'Збиток видалено');
  }

  private async change(command: () => Promise<unknown>, success: string): Promise<boolean> {
    this.saving.set(true);
    try {
      await command();
      this.messages.add({ severity: 'success', summary: success });
      this.lossesResource.reload();
      this.overviewResource.reload();
      this.detailsResource.reload();
      return true;
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не збережено', detail: toProblemMessage(error) });
      return false;
    } finally {
      this.saving.set(false);
    }
  }
}
