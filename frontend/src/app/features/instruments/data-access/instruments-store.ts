import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { AccountHoldings, LatestPrice, symbolOf, toIsoDate } from '../../../shared/models/api';

export interface QuotePoint {
  date: string;
  close: number;
  adjustedClose: number;
  flags: string[];
}

interface QuoteSeries {
  instrumentId: string;
  points: QuotePoint[];
}

export type InstrumentFilter = 'all' | 'mine' | 'eu' | 'us' | 'stocks' | 'other';
export type ChartPeriod = '1M' | '3M' | '6M' | '1Y' | '3Y';

export interface InstrumentRow {
  id: string;
  symbol: string;
  name: string;
  isin: string | null;
  kind: string;
  group: string;
  exchange: string;
  currency: string;
  isAccumulating: boolean | null;
  price: LatestPrice | null;
  /** Change over the last month (adjusted close), for the list. */
  change1m: number | null;
  /** Last ~30 closes for the sparkline. */
  spark: number[];
  held: boolean;
}

const periodDays: Record<ChartPeriod, number> = { '1M': 31, '3M': 92, '6M': 183, '1Y': 366, '3Y': 1096 };

function daysAgo(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return toIsoDate(date);
}

/** The catalog as a market screen: latest price, a month of history per instrument, a detailed chart for the chosen one. */
@Injectable()
export class InstrumentsStore {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);
  private readonly catalog = inject(InstrumentCatalog);
  private readonly accounts = inject(AccountContext);

  readonly filter = signal<InstrumentFilter>('all');
  readonly search = signal('');
  readonly selectedId = signal<string | null>(null);
  readonly period = signal<ChartPeriod>('1Y');
  readonly busy = signal<'quotes' | 'fx' | null>(null);

  private readonly ids = computed(() => this.catalog.instruments().map((instrument) => instrument.id));

  private readonly pricesResource = resource({
    params: () => this.ids(),
    loader: ({ params: ids }) => (ids.length ? this.api.get<LatestPrice[]>('/api/market-data/quotes/latest', { instrumentIds: ids }) : Promise.resolve([])),
  });

  private readonly monthResource = resource({
    params: () => this.ids(),
    loader: ({ params: ids }) =>
      ids.length ? this.api.get<QuoteSeries[]>('/api/market-data/quotes/history', { instrumentIds: ids, from: daysAgo(35), to: daysAgo(0) }) : Promise.resolve([]),
  });

  private readonly holdingsResource = resource({
    params: () => this.accounts.currentId() ?? undefined,
    loader: ({ params: accountId }) => this.api.get<AccountHoldings>(`/api/portfolio/accounts/${accountId}/holdings`),
  });

  private readonly detailResource = resource({
    params: () => (this.selectedId() ? { id: this.selectedId()!, period: this.period() } : undefined),
    loader: ({ params }) =>
      this.api.get<QuoteSeries>(`/api/market-data/quotes/${params.id}`, { from: daysAgo(periodDays[params.period]), to: daysAgo(0) }),
  });

  readonly isLoading = computed(() => this.pricesResource.isLoading());
  readonly detailLoading = this.detailResource.isLoading;

  private readonly heldIds = computed(
    () => new Set((this.holdingsResource.value()?.subPortfolios ?? []).flatMap((sub) => sub.positions.filter((position) => position.quantity > 0).map((position) => position.instrumentId))),
  );

  readonly rows = computed<InstrumentRow[]>(() => {
    const prices = new Map((this.pricesResource.value() ?? []).map((price) => [price.instrumentId, price]));
    const months = new Map((this.monthResource.value() ?? []).map((series) => [series.instrumentId, series.points]));
    const held = this.heldIds();
    return this.catalog
      .instruments()
      .filter((instrument) => instrument.kind !== 'FxPair')
      .map((instrument) => {
        const points = months.get(instrument.id) ?? [];
        const first = points.at(0)?.adjustedClose;
        const last = points.at(-1)?.adjustedClose;
        return {
          id: instrument.id,
          symbol: symbolOf(instrument),
          name: instrument.name,
          isin: instrument.isin,
          kind: instrument.kind,
          group: instrument.group ?? '',
          exchange: instrument.exchange,
          currency: instrument.tradingCurrency,
          isAccumulating: instrument.isAccumulating,
          price: prices.get(instrument.id) ?? null,
          change1m: first && last ? last / first - 1 : null,
          spark: points.map((point) => point.close),
          held: held.has(instrument.id),
        };
      })
      .sort((left, right) => Number(right.held) - Number(left.held) || left.kind.localeCompare(right.kind) || left.group.localeCompare(right.group) || left.symbol.localeCompare(right.symbol));
  });

  readonly visibleRows = computed(() => {
    const text = this.search().trim().toLowerCase();
    const filter = this.filter();
    return this.rows().filter((row) => {
      if (text && !`${row.symbol} ${row.name} ${row.isin ?? ''}`.toLowerCase().includes(text)) return false;
      switch (filter) {
        case 'mine': return row.held;
        case 'eu': return row.kind === 'Etf' && row.currency === 'EUR';
        case 'us': return row.kind === 'Etf' && row.currency === 'USD';
        case 'stocks': return row.kind === 'Stock';
        case 'other': return row.kind !== 'Etf' && row.kind !== 'Stock';
        default: return true;
      }
    });
  });

  readonly selected = computed(() => this.rows().find((row) => row.id === this.selectedId()) ?? null);
  readonly detailPoints = computed(() => this.detailResource.value()?.points ?? []);

  /** Period statistics of the chosen instrument (adjusted close: dividends of distributing ETFs count as return). */
  readonly detailStats = computed(() => {
    const points = this.detailPoints();
    if (points.length < 2) return null;
    const closes = points.map((point) => point.close);
    const first = points[0].adjustedClose;
    const last = points.at(-1)!.adjustedClose;
    let peak = -Infinity;
    let maxDrawdown = 0;
    for (const point of points) {
      peak = Math.max(peak, point.adjustedClose);
      maxDrawdown = Math.min(maxDrawdown, point.adjustedClose / peak - 1);
    }
    const daily = points.slice(1).map((point, index) => point.adjustedClose / points[index].adjustedClose - 1);
    const mean = daily.reduce((sum, value) => sum + value, 0) / daily.length;
    const variance = daily.reduce((sum, value) => sum + (value - mean) ** 2, 0) / Math.max(1, daily.length - 1);
    return {
      change: last / first - 1,
      high: Math.max(...closes),
      low: Math.min(...closes),
      maxDrawdown,
      volatility: Math.sqrt(variance) * Math.sqrt(252),
      flagged: points.filter((point) => point.flags.length > 0).length,
    };
  });

  readonly lastQuoteDate = computed(() => (this.pricesResource.value() ?? []).map((price) => price.date).sort().at(-1) ?? null);
  readonly counts = computed(() => {
    const rows = this.rows();
    return {
      all: rows.length,
      mine: rows.filter((row) => row.held).length,
      eu: rows.filter((row) => row.kind === 'Etf' && row.currency === 'EUR').length,
      us: rows.filter((row) => row.kind === 'Etf' && row.currency === 'USD').length,
      stocks: rows.filter((row) => row.kind === 'Stock').length,
      other: rows.filter((row) => row.kind !== 'Etf' && row.kind !== 'Stock').length,
    };
  });

  select(id: string): void {
    this.selectedId.set(this.selectedId() === id ? null : id);
  }

  async ingestQuotes(): Promise<void> {
    await this.run('quotes', async () => {
      const report = await this.api.post<{ quotesAdded: number; quotesUpdated: number; missingSymbols: string[] }>('/api/market-data/quotes/ingest');
      this.pricesResource.reload();
      this.monthResource.reload();
      this.detailResource.reload();
      const missing = report.missingSymbols.length ? `; без даних: ${report.missingSymbols.join(', ')}` : '';
      return `Нових котирувань: ${report.quotesAdded}, оновлено: ${report.quotesUpdated}${missing}`;
    });
  }

  async ingestFxRates(): Promise<void> {
    await this.run('fx', async () => {
      await this.api.post('/api/market-data/fx-rates/ingest');
      return 'Курси NBP оновлено';
    });
  }

  private async run(kind: 'quotes' | 'fx', command: () => Promise<string>): Promise<void> {
    this.busy.set(kind);
    try {
      this.messages.add({ severity: 'success', summary: await command() });
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося оновити', detail: toProblemMessage(error), life: 8000 });
    } finally {
      this.busy.set(null);
    }
  }
}
