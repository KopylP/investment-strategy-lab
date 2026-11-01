import { computed, inject, Injectable, resource } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { AuthStore } from '../auth/auth-store';

export interface TaxRegimeDto {
  taxCurrency: string;
  fxSource: string;
  rateLabel: string;
  summary: string | null;
  estimateNote: string | null;
  taxedAccountsLabel: string | null;
  overviewHint: string | null;
  progressNote: string | null;
  lossesNote: string | null;
}

@Injectable({ providedIn: 'root' })
export class TaxRegime {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthStore);

  private readonly regimeResource = resource({
    params: () => (this.auth.isSignedIn() ? true : undefined),
    loader: () => this.api.get<TaxRegimeDto>('/api/taxation/regime'),
  });

  readonly regime = computed(() => (this.regimeResource.hasValue() ? this.regimeResource.value() : null));
  readonly currency = computed(() => this.regime()?.taxCurrency ?? null);

  reload(): void {
    this.regimeResource.reload();
  }
}
