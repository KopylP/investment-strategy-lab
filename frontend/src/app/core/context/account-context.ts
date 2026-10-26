import { computed, effect, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { AccountSetup } from '../../shared/models/api';

const storageKey = 'investmentlab.account';
const allKey = 'investmentlab.account.all';

/**
 * How a screen relates to the account choice (route data `accountScope`): `account` (default) works on one account,
 * `all` can also show every account together, `owner` is about the person (taxes: one PIT-38), not an account.
 */
export type AccountScope = 'account' | 'all' | 'owner';

/** The user's accounts and the one the screens are about (remembered per browser), or all of them together. */
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

  /** The single account of single-account screens; in the all-accounts mode, the one chosen last. */
  readonly current = computed<AccountSetup | undefined>(() => {
    const accounts = this.accounts();
    return accounts.find((account) => account.id === this.chosenId()) ?? accounts[0];
  });

  readonly currentId = computed(() => this.current()?.id);

  /** All accounts together — only meaningful with more than one; screens with the `all` scope honour it. */
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

  /** A screen about one account (a report, an account page) makes it current without leaving the all-accounts mode. */
  focus(accountId: string): void {
    this.chosenId.set(accountId);
  }

  reload(): void {
    this.accountsResource.reload();
  }

  /** After sign-out another user may sign in in the same tab. */
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
    /* private mode: the choice is just not remembered */
  }
}
