import { computed, effect, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { AuthStore } from '../auth/auth-store';

interface LatestFxRates {
  effectiveDate: string | null;
  toPln: Record<string, number>;
}

@Injectable({ providedIn: 'root' })
export class DisplayCurrency {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthStore);

  private readonly chosen = signal<string | null>(null);

  private readonly profileResource = resource({
    params: () => (this.auth.isSignedIn() ? this.auth.user()?.id : undefined),
    loader: () => this.auth.getProfile(),
  });

  private readonly ratesResource = resource({
    params: () => (this.auth.isSignedIn() ? true : undefined),
    loader: () => this.api.get<LatestFxRates>('/api/market-data/fx-rates/latest'),
  });

  readonly code = computed(
    () => this.chosen() ?? this.profileResource.value()?.displayCurrency ?? null,
  );
  readonly ratesDate = computed(() => this.ratesResource.value()?.effectiveDate ?? null);
  readonly options = ['EUR', 'USD', 'PLN'];

  constructor() {
    effect(() => {
      if (!this.auth.isSignedIn()) this.chosen.set(null);
    });
  }

  rate(from: string, to: string): number | null {
    if (from === to) return 1;

    const toPln = this.ratesResource.value()?.toPln;
    const fromPln = toPln?.[from];
    const targetPln = toPln?.[to];

    return fromPln && targetPln ? fromPln / targetPln : null;
  }

  convert(amount: number | null | undefined, from: string): number | null {
    const target = this.code();
    if (amount === null || amount === undefined || !target) return null;

    const rate = this.rate(from, target);

    return rate === null ? null : amount * rate;
  }

  async choose(code: string): Promise<void> {
    this.chosen.set(code);
    const profile = this.profileResource.value() ?? (await this.auth.getProfile());
    const saved = await this.auth.updateProfile({ ...profile, displayCurrency: code });

    this.profileResource.set(saved);
  }

  reloadRates(): void {
    this.ratesResource.reload();
  }
}
