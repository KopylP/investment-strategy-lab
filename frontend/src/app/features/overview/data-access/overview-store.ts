import { TaxRegime } from '../../../core/context/tax-regime';
import { computed, inject, Injectable, linkedSignal, resource, signal } from '@angular/core';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { DisplayCurrency } from '../../../core/context/display-currency';
import { DashboardDto, ReportSummaryDto, StrategyDto, TaxProgress } from '../models/overview-dto';

export type DashboardPeriod = '1M' | '3M' | '6M' | '1Y' | 'YTD' | 'ALL';

@Injectable()
export class OverviewStore {
  private readonly api = inject(ApiClient);
  private readonly regime = inject(TaxRegime);
  private readonly accounts = inject(AccountContext);
  private readonly display = inject(DisplayCurrency);

  readonly period = signal<DashboardPeriod>('ALL');

  private readonly strategiesResource = resource({
    loader: () => this.api.get<StrategyDto[]>('/api/strategies'),
  });

  private readonly dashboardResource = resource({
    params: () => {
      if (this.accounts.isAll())
        return {
          url: '/api/performance/dashboard',
          currency: this.display.code() ?? undefined,
          period: this.period(),
        };

      const accountId = this.accounts.currentId();

      return accountId
        ? {
            url: `/api/performance/accounts/${accountId}/dashboard`,
            currency: undefined,
            period: this.period(),
          }
        : undefined;
    },
    loader: ({ params }) =>
      this.api.get<DashboardDto>(params.url, { period: params.period, currency: params.currency }),
  });

  private readonly reportsResource = resource({
    params: () =>
      this.accounts.isAll()
        ? { all: true }
        : this.accounts.currentId()
          ? { accountId: this.accounts.currentId() }
          : undefined,
    loader: ({ params }) =>
      this.api.get<ReportSummaryDto[]>(
        '/api/rebalancing/reports',
        'accountId' in params ? { accountId: params.accountId } : undefined,
      ),
  });

  readonly taxYear = new Date().getFullYear();
  private readonly taxResource = resource({
    params: () => (this.accounts.current() ? this.taxYear : undefined),
    loader: ({ params: year }) => this.api.get<TaxProgress>(`/api/taxation/progress/${year}`),
  });

  private readonly totalResource = resource({
    params: () =>
      this.tax()?.alertShare != null ? (this.regime.currency() ?? undefined) : undefined,
    loader: ({ params: currency }) =>
      this.api.get<DashboardDto>('/api/performance/dashboard', { currency, period: '1M' }),
  });

  readonly accountsLoaded = this.accounts.hasLoaded;
  readonly account = this.accounts.current;
  readonly isAll = this.accounts.isAll;
  readonly allAccounts = this.accounts.accounts;
  readonly hasStrategies = computed(() => (this.strategiesResource.value()?.length ?? 0) > 0);
  readonly strategiesLoaded = computed(() => this.strategiesResource.hasValue());
  readonly withoutStrategy = computed(() => this.accountsInView().filter(hasNoActiveStrategy));

  private accountsInView() {
    if (this.isAll()) return this.allAccounts();

    const account = this.account();

    return account ? [account] : [];
  }

  readonly dashboard = linkedSignal<
    { value: DashboardDto | undefined; loading: boolean; accountId: string | null },
    DashboardDto | undefined
  >({
    source: () => ({
      value: this.dashboardResource.hasValue() ? this.dashboardResource.value() : undefined,
      loading: this.dashboardResource.isLoading(),
      accountId: this.accounts.currentId() ?? null,
    }),
    computation: (next, previous) =>
      next.value ??
      (next.loading && previous?.value?.accountId === next.accountId ? previous.value : undefined),
  });
  readonly isLoading = this.dashboardResource.isLoading;
  readonly loadError = this.dashboardResource.error;
  readonly loadErrorMessage = computed(() =>
    this.dashboardResource.error() ? toProblemMessage(this.dashboardResource.error()) : null,
  );
  readonly gainPct = computed(() => {
    const d = this.dashboard();

    return d && d.netContributions > 0 ? d.gain / d.netContributions : null;
  });
  readonly heldGain = computed(() => {
    const d = this.dashboard();
    if (!d || !d.costBasis) return null;

    const amount = d.positionsValue - d.costBasis;

    return { amount, pct: amount / d.costBasis, costBasis: d.costBasis };
  });
  readonly hasHistory = computed(() => (this.dashboard()?.series.length ?? 0) > 0);

  readonly reports = computed(() => this.reportsResource.value() ?? []);
  readonly openReports = computed(() =>
    this.reports().filter(
      (report) =>
        report.kind !== 'RiskReview' && (report.status === 'New' || report.status === 'InProgress'),
    ),
  );
  readonly latestRisk = computed(() =>
    this.reports().find((report) => report.kind === 'RiskReview'),
  );
  readonly planGap = computed(() => {
    const dashboard = this.dashboard();

    return dashboard?.planValue != null ? dashboard.value - dashboard.planValue : null;
  });

  readonly tax = computed(() =>
    this.taxResource.hasValue() ? this.taxResource.value() : undefined,
  );
  readonly taxError = computed(() =>
    this.taxResource.error() ? toProblemMessage(this.taxResource.error()) : null,
  );
  readonly allAccountsValueInTaxCurrency = computed(() =>
    this.totalResource.hasValue() ? (this.totalResource.value()?.value ?? null) : null,
  );
  readonly taxAlert = computed(() => {
    const tax = this.tax();
    const value = this.allAccountsValueInTaxCurrency();
    if (!tax || tax.alertShare === null || !value) return null;

    const limit = value * tax.alertShare;

    return {
      share: tax.alertShare,
      limit,
      ratio: tax.taxDue / value,
      exceeded: tax.taxDue > limit,
    };
  });
  readonly taxLast30Days = computed(() => {
    const tax = this.tax();
    if (!tax) return 0;

    const since = new Date();

    since.setDate(since.getDate() - 30);
    const before =
      [...tax.points].reverse().find((point) => new Date(point.date) < since)?.taxDue ?? 0;

    return tax.taxDue - before;
  });
  accountName(id: string): string {
    return this.allAccounts().find((account) => account.id === id)?.name ?? '';
  }

  showAccount(id: string): void {
    this.accounts.choose(id);
  }
}

function hasNoActiveStrategy(account: {
  subPortfolios: { status: string; strategyId: unknown }[];
}) {
  return !account.subPortfolios.some((sub) => sub.status === 'Active' && sub.strategyId);
}
