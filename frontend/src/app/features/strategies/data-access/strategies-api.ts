import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { Strategy, StrategySetupInput, StrategyType } from '../models/strategy';

@Injectable({ providedIn: 'root' })
export class StrategiesApi {
  private readonly api = inject(ApiClient);

  list(): Promise<Strategy[]> {
    return this.api.get<Strategy[]>('/api/strategies');
  }

  get(id: string, version?: number): Promise<Strategy> {
    return this.api.get<Strategy>(`/api/strategies/${id}`, { version });
  }

  catalog(): Promise<StrategyType[]> {
    return this.api.get<StrategyType[]>('/api/strategies/catalog');
  }

  createDefaults(): Promise<Strategy[]> {
    return this.api.post<Strategy[]>('/api/strategies/defaults', {});
  }

  reconfigure(id: string, setup: StrategySetupInput): Promise<Strategy> {
    return this.api.put<Strategy>(`/api/strategies/${id}`, setup);
  }

  archive(id: string): Promise<void> {
    return this.api.post(`/api/strategies/${id}/archive`);
  }
}
