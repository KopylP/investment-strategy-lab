import { computed, inject, Injectable, resource } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { AccountContext } from '../context/account-context';

@Injectable({ providedIn: 'root' })
export class OpenActionsBadge {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);

  private readonly countResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.get<{ count: number }>(`/api/rebalancing/accounts/${accountId}/open-actions`),
  });

  readonly count = computed(() => this.countResource.value()?.count ?? 0);

  refresh(): void {
    this.countResource.reload();
  }
}
