import { computed, inject, Injectable, resource } from '@angular/core';
import { AuthStore } from '../auth/auth-store';

export type ChartStyleName = 'Line' | 'Candles';

export const chartStyles: ChartStyleName[] = ['Line', 'Candles'];

@Injectable({ providedIn: 'root' })
export class ChartStyle {
  private readonly auth = inject(AuthStore);

  private readonly profileResource = resource({
    params: () => (this.auth.isSignedIn() ? this.auth.user()?.id : undefined),
    loader: () => this.auth.getProfile(),
  });

  readonly choice = computed<ChartStyleName | undefined>(() => {
    const profile = this.profileResource.value();

    return profile === undefined ? undefined : profile.chartStyle === 'Candles' ? 'Candles' : 'Line';
  });

  readonly isCandles = computed(() => this.choice() === 'Candles');

  async choose(style: ChartStyleName): Promise<void> {
    const profile = await this.auth.getProfile();
    const saved = await this.auth.updateProfile({ ...profile, chartStyle: style });

    this.profileResource.set(saved);
  }
}
