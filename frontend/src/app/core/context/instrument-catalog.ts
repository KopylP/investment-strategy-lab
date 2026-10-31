import { computed, inject, Injectable, resource } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { Instrument, symbolOf } from '../../shared/models/api';

@Injectable({ providedIn: 'root' })
export class InstrumentCatalog {
  private readonly api = inject(ApiClient);

  private readonly instrumentsResource = resource({
    loader: () => this.api.get<Instrument[]>('/api/market-data/instruments'),
  });

  readonly instruments = computed(() => this.instrumentsResource.value() ?? []);
  readonly byId = computed(
    () => new Map(this.instruments().map((instrument) => [instrument.id, instrument])),
  );
  readonly etfs = computed(() =>
    this.instruments().filter((instrument) => instrument.kind === 'Etf'),
  );

  symbol(id: string | null | undefined): string {
    if (!id) return '—';

    const instrument = this.byId().get(id);

    return instrument ? symbolOf(instrument) : id.slice(0, 8);
  }

  name(id: string | null | undefined): string {
    return (id && this.byId().get(id)?.name) || '';
  }
}
