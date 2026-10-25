import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountSetup } from '../../../shared/models/api';
import { Strategy } from '../../strategies/models/strategy';

export interface SubPortfolioInput {
  name: string;
  targetSharePercent: number;
  strategyId: string | null;
  pinnedStrategyVersion?: number | null;
  cashInstrumentId: string | null;
  maxPositionWeightPercent: number | null;
  maxMonthlyTurnoverPercent?: number | null;
}

export interface OpenAccountInput {
  name: string;
  broker: string;
  baseCurrency: string;
  taxPolicyCode?: string;
  subPortfolios: SubPortfolioInput[];
  contributionPolicy: { kind: string; driftBandPercent: number | null };
}

export interface PlanSettings {
  accountId?: string;
  monthlyContribution: number;
  minTrade: number;
  trimBand: number;
  allowFractionalShares: boolean;
  generateOnSchedule: boolean;
}

/** Strategies as the account screens use them (the full strategy from the Strategies API). */
export type StrategyOption = Strategy;

@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly api = inject(ApiClient);

  get(id: string): Promise<AccountSetup> {
    return this.api.get<AccountSetup>(`/api/portfolio/accounts/${id}`);
  }

  open(input: OpenAccountInput): Promise<AccountSetup> {
    return this.api.post<AccountSetup>('/api/portfolio/accounts', input);
  }

  changeTargetShares(id: string, sharesPercent: Record<string, number>): Promise<AccountSetup> {
    return this.api.put<AccountSetup>(`/api/portfolio/accounts/${id}/target-shares`, { targetShares: sharesPercent });
  }

  changeContributionPolicy(id: string, kind: string, driftBandPercent: number | null): Promise<AccountSetup> {
    return this.api.put<AccountSetup>(`/api/portfolio/accounts/${id}/contribution-policy`, { kind, driftBandPercent });
  }

  assignStrategy(id: string, subPortfolioId: string, strategyId: string | null, pinnedVersion: number | null): Promise<AccountSetup> {
    return this.api.put<AccountSetup>(`/api/portfolio/accounts/${id}/sub-portfolios/${subPortfolioId}/strategy`, { strategyId, pinnedVersion });
  }

  addSubPortfolio(id: string, subPortfolio: SubPortfolioInput, targetSharesOfOthers: Record<string, number>): Promise<AccountSetup> {
    return this.api.post<AccountSetup>(`/api/portfolio/accounts/${id}/sub-portfolios`, { subPortfolio, targetSharesOfOthers });
  }

  retire(id: string, subPortfolioId: string, targetSharesOfRemaining: Record<string, number>): Promise<AccountSetup> {
    return this.api.post<AccountSetup>(`/api/portfolio/accounts/${id}/sub-portfolios/${subPortfolioId}/retire`, { targetSharesOfRemaining });
  }

  planSettings(id: string): Promise<PlanSettings> {
    return this.api.get<PlanSettings>(`/api/rebalancing/accounts/${id}/settings`);
  }

  changePlanSettings(id: string, settings: PlanSettings): Promise<PlanSettings> {
    return this.api.put<PlanSettings>(`/api/rebalancing/accounts/${id}/settings`, settings);
  }

  strategies(): Promise<StrategyOption[]> {
    return this.api.get<StrategyOption[]>('/api/strategies');
  }

  createDefaultStrategies(): Promise<StrategyOption[]> {
    return this.api.post<StrategyOption[]>('/api/strategies/defaults', {});
  }

  importPositions(id: string, subPortfolioId: string, positionsJson: unknown, asOf: string): Promise<unknown> {
    return this.api.post(`/api/portfolio/accounts/${id}/sub-portfolios/${subPortfolioId}/import-positions`, positionsJson, { asOf });
  }
}

/** Shares in percent that add up to exactly 100 with two decimals; the rest of rounding goes to the largest. */
export function normalizeToHundred(weights: Record<string, number>): Record<string, number> {
  const total = Object.values(weights).reduce((sum, weight) => sum + weight, 0);
  const keys = Object.keys(weights);
  if (!keys.length || total <= 0) return weights;
  const result: Record<string, number> = {};
  for (const key of keys) result[key] = Math.round((weights[key] / total) * 10000) / 100;
  const rest = Math.round((100 - Object.values(result).reduce((sum, value) => sum + value, 0)) * 100) / 100;
  const largest = keys.reduce((best, key) => (result[key] > result[best] ? key : best), keys[0]);
  result[largest] = Math.round((result[largest] + rest) * 100) / 100;
  return result;
}
