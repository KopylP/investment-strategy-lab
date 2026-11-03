import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { AccountContext } from '../../../../core/context/account-context';
import { StrategiesStore } from '../../data-access/strategies-store';
import { StrategyCard } from '../../ui/strategy-card/strategy-card';

@Component({
  selector: 'app-strategy-list-page',
  imports: [Button, Message, Skeleton, PageHeader, EmptyState, StrategyCard],
  providers: [StrategiesStore],
  templateUrl: './strategy-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyListPage {
  protected readonly store = inject(StrategiesStore);
  private readonly accounts = inject(AccountContext);

  protected readonly active = computed(() =>
    this.store.strategies().filter((strategy) => strategy.status !== 'Archived'),
  );
  protected readonly archived = computed(() =>
    this.store.strategies().filter((strategy) => strategy.status === 'Archived'),
  );

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
}
