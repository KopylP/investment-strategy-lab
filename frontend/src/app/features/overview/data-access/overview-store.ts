import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DisplayCurrency } from '../../../core/context/display-currency';
import { TaxOverview } from '../../taxes/data-access/taxes-store';
import { DashboardDto } from './dashboard-api';

interface ReportSummary {
  id: string;
  accountId: string;
  period: string;
  kind: string;
  status: string;
  actionsOpen: number;
  actionsTotal: number;
  generatedAt: string;
}

export type DashboardPeriod = '1M' | '3M' | '6M' | '1Y' | 'YTD' | 'ALL';

/** The home screen: setup progress, what needs attention now, and how the money did (Performance) — one account or all. */
@Injectable()
export class OverviewStore {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);
  private readonly display = inject(DisplayCurrency);

  readonly period = signal<DashboardPeriod>('ALL');

  private readonly strategiesResource = resource({ loader: () => this.api.get<unknown[]>('/api/strategies') });

  private readonly dashboardResource = resource({
    params: () => {
      if (this.accounts.isAll()) return { url: '/api/performance/dashboard', currency: this.display.code() ?? undefined, period: this.period() };
      const accountId = this.accounts.currentId();
      return accountId ? { url: `/api/performance/accounts/${accountId}/dashboard`, currency: undefined, period: this.period() } : undefined;
    },
    loader: ({ params }) => this.api.get<DashboardDto>(params.url, { period: params.period, currency: params.currency }),
  });

  private readonly reportsResource = resource({
    params: () => (this.accounts.isAll() ? { all: true } : this.accounts.currentId() ? { accountId: this.accounts.currentId() } : undefined),
    loader: ({ params }) => this.api.get<ReportSummary[]>('/api/rebalancing/reports', 'accountId' in params ? { accountId: params.accountId } : undefined),
  });

  /** Tax is the person's (one PIT-38 over all accounts), whichever account is shown. */
  private readonly taxResource = resource({
    params: () => (this.accounts.current() ? true : undefined),
    loader: () => this.api.get<TaxOverview>('/api/taxation/years'),
  });

  readonly accountsLoaded = this.accounts.hasLoaded;
  readonly account = this.accounts.current;
  readonly isAll = this.accounts.isAll;
  readonly allAccounts = this.accounts.accounts;
  readonly hasStrategies = computed(() => (this.strategiesResource.value()?.length ?? 0) > 0);
  readonly strategiesLoaded = computed(() => this.strategiesResource.hasValue());
  /** Accounts on screen whose active sub-portfolios have no strategy: their reports would not know what to buy. */
  readonly withoutStrategy = computed(() =>
    (this.isAll() ? this.allAccounts() : this.account() ? [this.account()!] : []).filter(
      (account) => !account.subPortfolios.some((sub) => sub.status === 'Active' && sub.strategyId),
    ),
  );

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
  readonly openReports = computed(() => this.reports().filter((report) => report.kind !== 'RiskReview' && (report.status === 'New' || report.status === 'InProgress')));
  readonly latestRisk = computed(() => this.reports().find((report) => report.kind === 'RiskReview'));
  readonly planGap = computed(() => {
    const dashboard = this.dashboard();
    return dashboard?.planValue != null ? dashboard.value - dashboard.planValue : null;
  });

  /** This year's PIT-38 so far: realised gains and dividends of every taxed account. */
  readonly taxYear = new Date().getFullYear();
  readonly tax = computed(() => {
    const overview = this.taxResource.hasValue() ? this.taxResource.value() : undefined;
    if (!overview) return null;
    const taxed = overview.accounts.filter((account) => account.isTaxed);
    if (!taxed.length) return { taxed: 0, summary: null };
    return { taxed: taxed.length, summary: overview.years.find((year) => year.year === this.taxYear)?.summary ?? null };
  });

  accountName(id: string): string {
    return this.allAccounts().find((account) => account.id === id)?.name ?? '';
  }

  showAccount(id: string): void {
    this.accounts.choose(id);
  }
}
