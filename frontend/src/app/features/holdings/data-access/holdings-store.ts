import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { AccountHoldings, LatestPrice, Lot, toIsoDate } from '../../../shared/models/api';

export interface PositionVm {
  instrumentId: string;
  symbol: string;
  name: string;
  quantity: number;
  averageCost: number;
  costBasis: number;
  price: number | null;
  priceDate: string | null;
  isStale: boolean;
  value: number;
  pnl: number;
  pnlPct: number | null;
  weight: number;
  lots: Lot[];
}

export interface SleeveVm {
  id: string;
  name: string;
  targetShare: number;
  actualShare: number;
  cash: number;
  value: number;
  costBasis: number;
  pnl: number;
  positions: PositionVm[];
}

/** What the account holds, valued at the latest quotes: per sub-portfolio, with virtual lots. */
@Injectable()
export class HoldingsStore {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);
  private readonly instruments = inject(InstrumentCatalog);

  readonly asOf = signal<Date | null>(null);

  private readonly holdingsResource = resource({
    params: () => (this.accounts.currentId() ? { accountId: this.accounts.currentId()!, asOf: this.asOf() ? toIsoDate(this.asOf()!) : undefined } : undefined),
    loader: async ({ params }) => {
      const holdings = await this.api.get<AccountHoldings>(`/api/portfolio/accounts/${params.accountId}/holdings`, { asOf: params.asOf });
      const ids = [...new Set(holdings.subPortfolios.flatMap((sub) => sub.positions.map((position) => position.instrumentId)))];
      const prices = ids.length ? await this.api.get<LatestPrice[]>('/api/market-data/quotes/latest', { instrumentIds: ids, asOf: params.asOf }) : [];
      return { holdings, prices: new Map(prices.map((price) => [price.instrumentId, price])) };
    },
  });

  readonly account = this.accounts.current;
  readonly isLoading = this.holdingsResource.isLoading;
  readonly loadError = this.holdingsResource.error;
  readonly currency = computed(() => this.holdingsResource.value()?.holdings.currency ?? this.account()?.baseCurrency ?? 'EUR');

  readonly sleeves = computed<SleeveVm[]>(() => {
    const data = this.holdingsResource.value();
    const account = this.account();
    if (!data || !account) return [];
    const targets = new Map(account.subPortfolios.map((sub) => [sub.id, sub.targetShare]));
    const sleeves = data.holdings.subPortfolios.map((sub) => {
      const positions = sub.positions.map((position) => {
        const quote = data.prices.get(position.instrumentId);
        const price = quote?.close ?? null;
        const value = price !== null ? position.quantity * price : position.costBasis;
        return {
          instrumentId: position.instrumentId,
          symbol: this.instruments.symbol(position.instrumentId),
          name: this.instruments.name(position.instrumentId),
          quantity: position.quantity,
          averageCost: position.averageCost,
          costBasis: position.costBasis,
          price,
          priceDate: quote?.date ?? null,
          isStale: quote?.isStale ?? true,
          value,
          pnl: value - position.costBasis,
          pnlPct: position.costBasis ? value / position.costBasis - 1 : null,
          weight: 0,
          lots: position.lots,
        };
      });
      const value = positions.reduce((sum, position) => sum + position.value, 0) + sub.cash;
      positions.forEach((position) => (position.weight = value ? position.value / value : 0));
      const costBasis = positions.reduce((sum, position) => sum + position.costBasis, 0);
      return {
        id: sub.subPortfolioId,
        name: sub.name,
        targetShare: targets.get(sub.subPortfolioId) ?? 0,
        actualShare: 0,
        cash: sub.cash,
        value,
        costBasis,
        pnl: positions.reduce((sum, position) => sum + position.pnl, 0),
        positions: positions.sort((left, right) => right.value - left.value),
      };
    });
    const total = sleeves.reduce((sum, sleeve) => sum + sleeve.value, 0);
    sleeves.forEach((sleeve) => (sleeve.actualShare = total ? sleeve.value / total : 0));
    return sleeves;
  });

  readonly totals = computed(() => {
    const sleeves = this.sleeves();
    const value = sleeves.reduce((sum, sleeve) => sum + sleeve.value, 0);
    const cash = sleeves.reduce((sum, sleeve) => sum + sleeve.cash, 0);
    const costBasis = sleeves.reduce((sum, sleeve) => sum + sleeve.costBasis, 0);
    const pnl = sleeves.reduce((sum, sleeve) => sum + sleeve.pnl, 0);
    return { value, cash, invested: value - cash, costBasis, pnl, pnlPct: costBasis ? pnl / costBasis : null };
  });

  readonly staleQuotes = computed(() => this.sleeves().some((sleeve) => sleeve.positions.some((position) => position.isStale)));
}
