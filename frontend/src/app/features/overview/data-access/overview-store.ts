import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DashboardDto } from './dashboard-api';

interface ReportSummary {
  id: string;
  period: string;
  kind: string;
  status: string;
  actionsOpen: number;
  actionsTotal: number;
  generatedAt: string;
}

export type DashboardPeriod = '1M' | '3M' | '6M' | '1Y' | 'YTD' | 'ALL';

/** The home screen: setup progress, what needs attention now, and how the money did (Performance). */
@Injectable()
export class OverviewStore {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);

  readonly period = signal<DashboardPeriod>('ALL');

  private readonly strategiesResource = resource({ loader: () => this.api.get<unknown[]>('/api/strategies') });

  private readonly dashboardResource = resource({
    params: () => (this.accounts.currentId() ? { accountId: this.accounts.currentId()!, period: this.period() } : undefined),
    loader: ({ params }) => this.api.get<DashboardDto>(`/api/performance/accounts/${params.accountId}/dashboard`, { period: params.period }),
  });

  private readonly reportsResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.get<ReportSummary[]>('/api/rebalancing/reports', { accountId }),
  });

  readonly accountsLoaded = this.accounts.hasLoaded;
  readonly account = this.accounts.current;
  readonly hasStrategies = computed(() => (this.strategiesResource.value()?.length ?? 0) > 0);
  readonly strategiesLoaded = computed(() => this.strategiesResource.hasValue());
  readonly hasStrategyAssigned = computed(() => this.account()?.subPortfolios.some((sub) => sub.status === 'Active' && sub.strategyId) ?? false);

  readonly dashboard = computed(() => (this.dashboardResource.hasValue() ? this.dashboardResource.value() : undefined));
  readonly isLoading = this.dashboardResource.isLoading;
  readonly loadError = this.dashboardResource.error;
  /** Gain over the money paid in, as a fraction of it (what a broker app shows next to the total). */
  readonly gainPct = computed(() => {
    const d = this.dashboard();
    return d && d.netContributions > 0 ? d.gain / d.netContributions : null;
  });
  readonly hasHistory = computed(() => (this.dashboard()?.series.length ?? 0) > 0);

  readonly reports = computed(() => this.reportsResource.value() ?? []);
  readonly openReport = computed(() => this.reports().find((report) => report.kind !== 'RiskReview' && (report.status === 'New' || report.status === 'InProgress')));
  readonly latestRisk = computed(() => this.reports().find((report) => report.kind === 'RiskReview'));
  readonly planGap = computed(() => {
    const dashboard = this.dashboard();
    return dashboard?.planValue != null ? dashboard.value - dashboard.planValue : null;
  });
}
