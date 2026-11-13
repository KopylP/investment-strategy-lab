import { computed, inject, Injectable, resource } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { ApiSchema } from '../api/api-schema';
import { AuthStore } from '../auth/auth-store';

@Injectable({ providedIn: 'root' })
export class TaxRegime {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthStore);

  private readonly regimeResource = resource({
    params: () => (this.auth.isSignedIn() ? true : undefined),
    loader: () => this.api.get<ApiSchema<'TaxRegimeDto'>>('/api/taxation/regime'),
  });

  readonly regime = computed(() => (this.regimeResource.hasValue() ? this.regimeResource.value() : null));
  readonly currency = computed(() => this.regime()?.taxCurrency ?? null);

  reload(): void {
    this.regimeResource.reload();
  }
}
