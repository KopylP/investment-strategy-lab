import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountSetup } from '../../../shared/models/account-setup-dto';
import {
  ChangeReportScheduleBody,
  ReportSchedule,
} from '../../reports/models/report-schedule-dto';
import { Strategy } from '../../strategies/models/strategy-dto';
import {
  AddSubPortfolioBody,
  AssignStrategyBody,
  ChangePlanSettingsBody,
  ChangeTargetSharesBody,
  ContributionPolicyChoice,
  ImportReport,
  NewSubPortfolioDto,
  OpenAccountBody,
  PlanSettingsDto,
  RenameSubPortfolioBody,
  RetireSubPortfolioBody,
  TaxPolicyDto,
} from '../models/account-dto';

export type StrategyOption = Strategy;

@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly api = inject(ApiClient);

  get(id: string): Promise<AccountSetup> {
    return this.api.get<AccountSetup>(`/api/portfolio/accounts/${id}`);
  }

  taxPolicies(): Promise<TaxPolicyDto[]> {
    return this.api.get<TaxPolicyDto[]>('/api/taxation/policies');
  }

  open(account: OpenAccountBody): Promise<AccountSetup> {
    return this.api.post<AccountSetup>('/api/portfolio/accounts', account);
  }

  changeTargetShares(id: string, sharesPercent: Record<string, number>): Promise<AccountSetup> {
    const body: ChangeTargetSharesBody = { targetShares: sharesPercent };

    return this.api.put<AccountSetup>(`/api/portfolio/accounts/${id}/target-shares`, body);
  }

  changeContributionPolicy(
    id: string,
    kind: string,
    driftBandPercent: number | null,
  ): Promise<AccountSetup> {
    const body: ContributionPolicyChoice = { kind, driftBandPercent };

    return this.api.put<AccountSetup>(`/api/portfolio/accounts/${id}/contribution-policy`, body);
  }

  assignStrategy(
    id: string,
    subPortfolioId: string,
    strategyId: string | null,
    pinnedVersion: number | null,
  ): Promise<AccountSetup> {
    const body: AssignStrategyBody = { strategyId, pinnedVersion };

    return this.api.put<AccountSetup>(
      `/api/portfolio/accounts/${id}/sub-portfolios/${subPortfolioId}/strategy`,
      body,
    );
  }

  addSubPortfolio(
    id: string,
    subPortfolio: NewSubPortfolioDto,
    targetSharesOfOthers: Record<string, number>,
  ): Promise<AccountSetup> {
    const body: AddSubPortfolioBody = { subPortfolio, targetSharesOfOthers };

    return this.api.post<AccountSetup>(`/api/portfolio/accounts/${id}/sub-portfolios`, body);
  }

  renameSubPortfolio(id: string, subPortfolioId: string, name: string): Promise<AccountSetup> {
    const body: RenameSubPortfolioBody = { name };

    return this.api.put<AccountSetup>(
      `/api/portfolio/accounts/${id}/sub-portfolios/${subPortfolioId}/name`,
      body,
    );
  }

  retire(
    id: string,
    subPortfolioId: string,
    targetSharesOfRemaining: Record<string, number>,
  ): Promise<AccountSetup> {
    const body: RetireSubPortfolioBody = { targetSharesOfRemaining };

    return this.api.post<AccountSetup>(
      `/api/portfolio/accounts/${id}/sub-portfolios/${subPortfolioId}/retire`,
      body,
    );
  }

  planSettings(id: string): Promise<PlanSettingsDto> {
    return this.api.get<PlanSettingsDto>(`/api/rebalancing/accounts/${id}/settings`);
  }

  changeReportSchedule(
    id: string,
    code: string,
    timing: ChangeReportScheduleBody,
  ): Promise<ReportSchedule> {
    return this.api.put<ReportSchedule>(
      `/api/rebalancing/accounts/${id}/report-schedule/${code}`,
      timing,
    );
  }

  changePlanSettings(id: string, settings: ChangePlanSettingsBody): Promise<PlanSettingsDto> {
    return this.api.put<PlanSettingsDto>(`/api/rebalancing/accounts/${id}/settings`, settings);
  }

  strategies(): Promise<StrategyOption[]> {
    return this.api.get<StrategyOption[]>('/api/strategies');
  }

  createDefaultStrategies(): Promise<StrategyOption[]> {
    return this.api.post<StrategyOption[]>('/api/strategies/defaults', {});
  }

  importPositions(
    id: string,
    subPortfolioId: string,
    positionsJson: unknown,
    asOf: string,
  ): Promise<ImportReport> {
    return this.api.post<ImportReport>(
      `/api/portfolio/accounts/${id}/sub-portfolios/${subPortfolioId}/import-positions`,
      positionsJson,
      { asOf },
    );
  }
}

export function normalizeToHundred(weights: Record<string, number>): Record<string, number> {
  const total = Object.values(weights).reduce((sum, weight) => sum + weight, 0);
  const keys = Object.keys(weights);
  if (!keys.length || total <= 0) return weights;

  const result: Record<string, number> = {};

  for (const key of keys) result[key] = Math.round((weights[key] / total) * 10000) / 100;

  const rest =
    Math.round((100 - Object.values(result).reduce((sum, value) => sum + value, 0)) * 100) / 100;
  const largest = keys.reduce((best, key) => (result[key] > result[best] ? key : best), keys[0]);

  result[largest] = Math.round((result[largest] + rest) * 100) / 100;

  return result;
}
