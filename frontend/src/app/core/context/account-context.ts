import { computed, effect, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { AccountSetup } from '../../shared/models/api';

const storageKey = 'investmentlab.account';
const allKey = 'investmentlab.account.all';

export type AccountScope = 'account' | 'all';

@Injectable({ providedIn: 'root' })
export class AccountContext {
  private readonly api = inject(ApiClient);
  private readonly chosenId = signal<string | null>(readStored(storageKey));
  private readonly allChosen = signal(readStored(allKey) === 'true');

  private readonly accountsResource = resource({
    loader: () => this.api.get<AccountSetup[]>('/api/portfolio/accounts'),
  });

  readonly accounts = computed(() => this.accountsResource.value() ?? []);
  readonly isLoading = this.accountsResource.isLoading;
  readonly hasLoaded = computed(() => this.accountsResource.hasValue());

  readonly current = computed<AccountSetup | undefined>(() => {
    const accounts = this.accounts();
    return accounts.find((account) => account.id === this.chosenId()) ?? accounts[0];
  });

  readonly currentId = computed(() => this.current()?.id);

  readonly isAll = computed(() => this.allChosen() && this.accounts().length > 1);

  constructor() {
    effect(() => {
      const id = this.current()?.id;
      if (id) writeStored(storageKey, id);
    });
    effect(() => writeStored(allKey, String(this.allChosen())));
  }

  choose(accountId: string): void {
    this.chosenId.set(accountId);
    this.allChosen.set(false);
  }

  chooseAll(): void {
    this.allChosen.set(true);
  }

  focus(accountId: string): void {
    this.chosenId.set(accountId);
  }

  reload(): void {
    this.accountsResource.reload();
  }

  forget(): void {
    this.chosenId.set(null);
    this.allChosen.set(false);
    this.accountsResource.set(undefined);
  }
}

function readStored(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeStored(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
  }
}
