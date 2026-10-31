import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { AccountContext } from '../../../../core/context/account-context';
import { StrategiesStore } from '../../data-access/strategies-store';
import { Strategy } from '../../models/strategy';
import { describeStrategy } from '../../models/strategy-summary';

@Component({
  selector: 'app-strategy-list-page',
  imports: [RouterLink, Button, Message, Skeleton, Tag, PageHeader, EmptyState, DatePipe],
  providers: [StrategiesStore],
  templateUrl: './strategy-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyListPage {
  protected readonly store = inject(StrategiesStore);
  private readonly accounts = inject(AccountContext);

  protected readonly usage = computed(() => {
    const usage = new Map<string, string[]>();
    for (const account of this.accounts.accounts())
      for (const sub of account.subPortfolios)
        if (sub.status === 'Active' && sub.strategyId)
          usage.set(sub.strategyId, [
            ...(usage.get(sub.strategyId) ?? []),
            `${account.name} › ${sub.name}`,
          ]);
    return usage;
  });

  protected blurb(strategy: Strategy) {
    return describeStrategy(
      strategy,
      (id) => this.store.instruments.symbol(id),
      (id) => this.store.instruments.byId().get(id)?.tradingCurrency,
    );
  }
}
