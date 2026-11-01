import { computed, DestroyRef, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { BacktestDto, MoneyOutcomesDto } from '../models/backtest-dto';

const POLL_MILLISECONDS = 5000;

@Injectable()
export class BacktestStore {
  private readonly api = inject(ApiClient);

  private readonly strategy = signal<{ id: string; version: number } | null>(null);

  readonly start = signal(10000);
  readonly contribution = signal(200);
  readonly years = signal(5);

  private readonly backtestResource = resource({
    params: () => this.strategy() ?? undefined,
    loader: ({ params }) =>
      this.api.get<BacktestDto>(`/api/backtesting/strategies/${params.id}`, {
        version: params.version,
      }),
  });

  readonly backtest = computed(() =>
    this.backtestResource.hasValue() ? this.backtestResource.value() : null,
  );
  readonly loadError = this.backtestResource.error;
  readonly isCalculating = computed(() => {
    const status = this.backtest()?.status;

    return status === 'Queued' || status === 'Running';
  });

  private readonly outcomesResource = resource({
    params: () => {
      const strategy = this.strategy();
      const hasResult = !!this.backtest()?.result;

      return strategy && hasResult
        ? {
            ...strategy,
            start: this.start(),
            contribution: this.contribution(),
            years: this.years(),
          }
        : undefined;
    },
    loader: ({ params }) =>
      this.api.get<MoneyOutcomesDto>(`/api/backtesting/strategies/${params.id}/money-outcomes`, {
        version: params.version,
        start: params.start,
        contribution: params.contribution,
        years: params.years,
      }),
  });

  readonly outcomes = computed(() =>
    this.outcomesResource.hasValue() ? this.outcomesResource.value() : null,
  );

  constructor() {
    const timer = setInterval(() => {
      if (this.isCalculating()) this.backtestResource.reload();
    }, POLL_MILLISECONDS);

    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  show(id: string, version: number): void {
    this.strategy.set({ id, version });
  }
}
