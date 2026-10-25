import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { LatestPrice, symbolOf } from '../../../shared/models/api';

/** The instrument catalog with the latest stored quote of each; manual refresh of quotes and NBP rates. */
@Injectable()
export class InstrumentsStore {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);
  private readonly catalog = inject(InstrumentCatalog);

  private readonly pricesResource = resource({
    params: () => this.catalog.instruments().map((instrument) => instrument.id),
    loader: ({ params: ids }) => (ids.length ? this.api.get<LatestPrice[]>('/api/market-data/quotes/latest', { instrumentIds: ids }) : Promise.resolve([])),
  });

  readonly busy = signal<'quotes' | 'fx' | null>(null);
  readonly isLoading = computed(() => this.pricesResource.isLoading());
  readonly rows = computed(() => {
    const prices = new Map((this.pricesResource.value() ?? []).map((price) => [price.instrumentId, price]));
    return this.catalog
      .instruments()
      .map((instrument) => ({
        id: instrument.id,
        symbol: symbolOf(instrument),
        name: instrument.name,
        isin: instrument.isin,
        kind: instrument.kind,
        group: instrument.group ?? '',
        exchange: instrument.exchange,
        currency: instrument.tradingCurrency,
        price: prices.get(instrument.id) ?? null,
      }))
      .sort((left, right) => left.kind.localeCompare(right.kind) || left.group.localeCompare(right.group) || left.symbol.localeCompare(right.symbol));
  });
  readonly lastQuoteDate = computed(() => (this.pricesResource.value() ?? []).map((price) => price.date).sort().at(-1) ?? null);

  async ingestQuotes(): Promise<void> {
    await this.run('quotes', async () => {
      const report = await this.api.post<{ quotesAdded: number; quotesUpdated: number; missingSymbols: string[] }>('/api/market-data/quotes/ingest');
      this.pricesResource.reload();
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
