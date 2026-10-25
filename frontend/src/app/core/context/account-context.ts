import { computed, effect, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { AccountSetup } from '../../shared/models/api';

const storageKey = 'investmentlab.account';

/** The user's accounts and the one the screens are about (remembered per browser). */
@Injectable({ providedIn: 'root' })
export class AccountContext {
  private readonly api = inject(ApiClient);
  private readonly chosenId = signal<string | null>(readStored());

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

  constructor() {
    effect(() => {
      const id = this.current()?.id;
      if (id) writeStored(id);
    });
  }

  choose(accountId: string): void {
    this.chosenId.set(accountId);
  }

  reload(): void {
    this.accountsResource.reload();
  }

  /** After sign-out another user may sign in in the same tab. */
  forget(): void {
    this.chosenId.set(null);
    this.accountsResource.set(undefined);
  }
}

function readStored(): string | null {
  try {
    return localStorage.getItem(storageKey);
  } catch {
    return null;
  }
}

function writeStored(id: string): void {
  try {
    localStorage.setItem(storageKey, id);
  } catch {
    /* private mode: the choice is just not remembered */
  }
}
