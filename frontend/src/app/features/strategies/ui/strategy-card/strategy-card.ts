import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Tag } from 'primeng/tag';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { StrategiesStore } from '../../data-access/strategies-store';
import { Strategy } from '../../models/strategy';
import { describeStrategy } from '../../models/strategy-summary';

@Component({
  selector: 'app-strategy-card',
  imports: [TranslocoPipe, RouterLink, Tag, DatePipe],
  templateUrl: './strategy-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyCard {
  readonly strategy = input.required<Strategy>();
  readonly places = input.required<string[]>();

  protected readonly store = inject(StrategiesStore);

  protected readonly blurb = computed(() =>
    describeStrategy(
      this.strategy(),
      (id) => this.store.instruments.symbol(id),
      (id) => this.store.instruments.byId().get(id)?.tradingCurrency,
    ),
  );
}
