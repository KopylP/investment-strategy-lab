import { computed, inject, Injectable, resource } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { ApiSchema } from '../../core/api/api-schema';
import { AccountContext } from '../../core/context/account-context';
import { TradeMarker } from '../charts/price-chart';

@Injectable({ providedIn: 'root' })
export class AccountTrades {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);

  private readonly journal = resource({
    params: () => this.accounts.currentId() ?? undefined,
    loader: ({ params: accountId }) =>
      this.api.get<ApiSchema<'TransactionDto'>[]>(
        `/api/portfolio/accounts/${accountId}/transactions`,
      ),
  });

  private readonly byInstrument = computed(() => {
    const currency = this.accounts.current()?.baseCurrency ?? 'EUR';
    const map = new Map<string, TradeMarker[]>();

    for (const entry of this.journal.hasValue() ? this.journal.value() : []) {
      if (!entry.trade || entry.isReversed || entry.kind === 'Reversal') continue;

      const marker: TradeMarker = {
        date: entry.occurredOn,
        side: entry.trade.side === 'Sell' ? 'Sell' : 'Buy',
        quantity: entry.trade.quantity,
        price: entry.trade.price,
        currency,
      };

      map.set(entry.trade.instrumentId, [...(map.get(entry.trade.instrumentId) ?? []), marker]);
    }

    return map;
  });

  of(instrumentId: string): TradeMarker[] {
    return this.byInstrument().get(instrumentId) ?? [];
  }

  reload(): void {
    this.journal.reload();
  }
}
