import { ChangeDetectionStrategy, Component, computed, inject, input, resource } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';
import { ApiClient } from '../../../core/api/api-client';
import { PriceChart } from '../../../shared/charts/price-chart';
import { AccountTrades } from '../../../shared/data/account-trades';
import { toIsoDate } from '../../../shared/models/api';

/** The position's price since a month before the first buy (at least a year), with every buy and sell marked. */
@Component({
  selector: 'app-position-chart',
  imports: [PriceChart, Skeleton],
  template: `
    @if (history.hasValue() && history.value().points.length > 1) {
      <app-price-chart [points]="history.value().points" [currency]="currency()" [trades]="markers()" height="15rem" />
    } @else if (history.isLoading()) {
      <p-skeleton height="15rem" />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PositionChart {
  private readonly api = inject(ApiClient);
  private readonly trades = inject(AccountTrades);

  readonly instrumentId = input.required<string>();
  /** Trading currency of the instrument: quotes are in it. */
  readonly currency = input.required<string>();

  protected readonly markers = computed(() => this.trades.of(this.instrumentId()));

  private readonly from = computed(() => {
    const first = this.markers().map((trade) => trade.date).sort()[0];
    const yearAgo = new Date();
    yearAgo.setFullYear(yearAgo.getFullYear() - 1);
    const start = first ? new Date(`${first}T00:00:00`) : yearAgo;
    start.setDate(start.getDate() - 30);
    return toIsoDate(start < yearAgo ? start : yearAgo);
  });

  protected readonly history = resource({
    params: () => ({ id: this.instrumentId(), from: this.from() }),
    loader: ({ params }) =>
      this.api.get<{ points: { date: string; close: number }[] }>(`/api/market-data/quotes/${params.id}`, { from: params.from, to: toIsoDate(new Date()) }),
  });
}
