import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DisplayCurrency } from '../../../core/context/display-currency';
import { ChartTheme } from '../../../shared/charts/chart-theme';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { AccountHoldings, LatestPrice, Lot, toIsoDate } from '../../../shared/models/api';

export interface PositionVm {
  instrumentId: string;
  symbol: string;
  name: string;
  description: string | null;
  quantity: number;
  averageCost: number;
  costBasis: number;
  price: number | null;
  quotedPrice: number | null;
  quoteCurrency: string;
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
  accountName: string | null;
  targetShare: number;
  actualShare: number;
  cash: number;
  cashParts: { currency: string; amount: number }[];
  value: number;
  costBasis: number;
  pnl: number;
  positions: PositionVm[];
}

@Injectable()
export class HoldingsStore {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);
  private readonly instruments = inject(InstrumentCatalog);
  private readonly fx = inject(DisplayCurrency);
  private readonly theme = inject(ChartTheme);

  readonly asOf = signal<Date | null>(null);

  private readonly holdingsResource = resource({
    params: () => {
      const accountIds = this.accounts.isAll() ? this.accounts.accounts().map((account) => account.id) : this.accounts.currentId() ? [this.accounts.currentId()!] : [];
      return accountIds.length ? { accountIds, asOf: this.asOf() ? toIsoDate(this.asOf()!) : undefined } : undefined;
    },
    loader: async ({ params }) => {
      const all = await Promise.all(params.accountIds.map((id) => this.api.get<AccountHoldings>(`/api/portfolio/accounts/${id}/holdings`, { asOf: params.asOf })));
      const ids = [...new Set(all.flatMap((holdings) => holdings.subPortfolios.flatMap((sub) => sub.positions.map((position) => position.instrumentId))))];
      const prices = ids.length ? await this.api.get<LatestPrice[]>('/api/market-data/quotes/latest', { instrumentIds: ids, asOf: params.asOf }) : [];
      return { all, prices: new Map(prices.map((price) => [price.instrumentId, price])) };
    },
  });

  readonly account = this.accounts.current;
  readonly isAll = this.accounts.isAll;
  readonly isLoading = this.holdingsResource.isLoading;
  readonly loadError = this.holdingsResource.error;
  readonly currency = computed(() =>
    this.isAll() ? (this.fx.code() ?? this.account()?.baseCurrency ?? 'EUR') : (this.holdingsResource.value()?.all[0]?.currency ?? this.account()?.baseCurrency ?? 'EUR'),
  );

  readonly sleeves = computed<SleeveVm[]>(() => {
    const data = this.holdingsResource.value();
    const currency = this.currency();
    if (!data) return [];
    return data.all.flatMap((holdings) => {
      const account = this.accounts.accounts().find((candidate) => candidate.id === holdings.accountId);
      const factor = holdings.currency === currency ? 1 : (this.fx.rate(holdings.currency, currency) ?? 1);
      return this.sleevesOf(holdings, currency, data.prices, account?.subPortfolios ?? [], factor, this.isAll() ? (account?.name ?? '') : null);
    });
  });

  private sleevesOf(
    holdings: AccountHoldings,
    currency: string,
    prices: Map<string, LatestPrice>,
    setup: { id: string; targetShare: number }[],
    factor: number,
    accountName: string | null,
  ): SleeveVm[] {
    const data = { holdings, prices };
    const targets = new Map(setup.map((sub) => [sub.id, sub.targetShare]));
    const sleeves = data.holdings.subPortfolios.map((sub) => {
      const positions = sub.positions.map((position) => {
        const quote = data.prices.get(position.instrumentId);
        const rate = quote ? this.fx.rate(quote.currency, currency) : null;
        const price = quote && rate !== null ? quote.close * rate : null;
        const value = price !== null ? position.quantity * price : position.costBasis * factor;
        return {
          instrumentId: position.instrumentId,
          symbol: this.instruments.symbol(position.instrumentId),
          name: this.instruments.name(position.instrumentId),
          description: this.instruments.byId().get(position.instrumentId)?.description ?? null,
          quantity: position.quantity,
          averageCost: position.averageCost * factor,
          costBasis: position.costBasis * factor,
          price,
          quotedPrice: quote?.close ?? null,
          quoteCurrency: quote?.currency ?? data.holdings.currency,
          priceDate: quote?.date ?? null,
          isStale: quote?.isStale ?? true,
          value,
          pnl: value - position.costBasis * factor,
          pnlPct: position.costBasis ? value / (position.costBasis * factor) - 1 : null,
          weight: 0,
          lots: factor === 1 ? position.lots : position.lots.map((lot) => ({ ...lot, unitCost: lot.unitCost * factor })),
        };
      });
      const foreign = sub.foreignCash ?? [];
      const cash = sub.cash * factor + foreign.reduce((sum, part) => sum + part.amount * (this.fx.rate(part.currency, currency) ?? 0), 0);
      const value = positions.reduce((sum, position) => sum + position.value, 0) + cash;
      positions.forEach((position) => (position.weight = value ? position.value / value : 0));
      const costBasis = positions.reduce((sum, position) => sum + position.costBasis, 0);
      return {
        id: sub.subPortfolioId,
        name: sub.name,
        accountName,
        targetShare: targets.get(sub.subPortfolioId) ?? 0,
        actualShare: 0,
        cash,
        cashParts: [{ currency: data.holdings.currency, amount: sub.cash }, ...foreign],
        value,
        costBasis,
        pnl: positions.reduce((sum, position) => sum + position.pnl, 0),
        positions: positions.sort((left, right) => right.value - left.value),
      };
    });
    const total = sleeves.reduce((sum, sleeve) => sum + sleeve.value, 0);
    sleeves.forEach((sleeve) => (sleeve.actualShare = total ? sleeve.value / total : 0));
    return sleeves;
  }

  readonly totals = computed(() => {
    const sleeves = this.sleeves();
    const value = sleeves.reduce((sum, sleeve) => sum + sleeve.value, 0);
    const cash = sleeves.reduce((sum, sleeve) => sum + sleeve.cash, 0);
    const parts = new Map<string, number>();
    for (const part of sleeves.flatMap((sleeve) => sleeve.cashParts)) parts.set(part.currency, (parts.get(part.currency) ?? 0) + part.amount);
    const cashParts = [...parts].map(([currency, amount]) => ({ currency, amount }));
    const costBasis = sleeves.reduce((sum, sleeve) => sum + sleeve.costBasis, 0);
    const pnl = sleeves.reduce((sum, sleeve) => sum + sleeve.pnl, 0);
    return { value, cash, cashParts, hasForeignCash: cashParts.length > 1, invested: value - cash, costBasis, pnl, pnlPct: costBasis ? pnl / costBasis : null };
  });

  readonly positionCount = computed(() => new Set(this.sleeves().flatMap((sleeve) => sleeve.positions.map((position) => position.instrumentId))).size);

  readonly composition = computed(() => {
    const total = this.totals().value;
    if (!total) return [];
    const byInstrument = new Map<string, { name: string; value: number }>();
    for (const position of this.sleeves().flatMap((sleeve) => sleeve.positions))
      byInstrument.set(position.symbol, { name: position.name, value: (byInstrument.get(position.symbol)?.value ?? 0) + position.value });
    const palette = this.theme.palette();
    const colors = [...palette.series, palette.gain, palette.tax];
    const slices = [...byInstrument]
      .sort((left, right) => right[1].value - left[1].value)
      .map(([label, { name, value }], index) => ({ label, name, weight: value / total, color: colors[index % colors.length] }));
    const cash = this.totals().cash;
    return cash > 0 ? [...slices, { label: 'Кеш', name: '', weight: cash / total, color: palette.neutral }] : slices;
  });

  readonly staleQuotes = computed(() => this.sleeves().some((sleeve) => sleeve.positions.some((position) => position.isStale)));
}
