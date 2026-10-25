import { computed, inject, Injectable, resource } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { AccountHoldings, LatestPrice } from '../../../shared/models/api';

interface ReportSummary {
  id: string;
  period: string;
  kind: string;
  status: string;
  actionsOpen: number;
  actionsTotal: number;
  generatedAt: string;
}

/** The home screen: where the user is in the setup and what needs attention now. */
@Injectable()
export class OverviewStore {
  private readonly api = inject(ApiClient);
  private readonly accounts = inject(AccountContext);

  private readonly strategiesResource = resource({ loader: () => this.api.get<unknown[]>('/api/strategies') });

  private readonly valuationResource = resource({
    params: () => this.accounts.currentId(),
    loader: async ({ params: accountId }) => {
      const holdings = await this.api.get<AccountHoldings>(`/api/portfolio/accounts/${accountId}/holdings`);
      const ids = [...new Set(holdings.subPortfolios.flatMap((sub) => sub.positions.map((position) => position.instrumentId)))];
      const prices = ids.length ? await this.api.get<LatestPrice[]>('/api/market-data/quotes/latest', { instrumentIds: ids }) : [];
      const close = new Map(prices.map((price) => [price.instrumentId, price.close]));
      return holdings.subPortfolios.map((sub) => {
        const invested = sub.positions.reduce((sum, position) => sum + position.quantity * (close.get(position.instrumentId) ?? position.averageCost), 0);
        const cost = sub.positions.reduce((sum, position) => sum + position.costBasis, 0);
        return { id: sub.subPortfolioId, name: sub.name, value: invested + sub.cash, cash: sub.cash, pnl: invested - cost, positions: sub.positions.length };
      });
    },
  });

  private readonly reportsResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.get<ReportSummary[]>('/api/rebalancing/reports', { accountId }),
  });

  readonly accountsLoaded = this.accounts.hasLoaded;
  readonly account = this.accounts.current;
  readonly hasStrategies = computed(() => (this.strategiesResource.value()?.length ?? 0) > 0);
  readonly strategiesLoaded = computed(() => this.strategiesResource.hasValue());
  readonly sleeves = computed(() => this.valuationResource.value() ?? []);
  readonly isLoading = computed(() => this.valuationResource.isLoading() || this.reportsResource.isLoading());
  readonly totalValue = computed(() => this.sleeves().reduce((sum, sleeve) => sum + sleeve.value, 0));
  readonly totalPnl = computed(() => this.sleeves().reduce((sum, sleeve) => sum + sleeve.pnl, 0));
  readonly totalCash = computed(() => this.sleeves().reduce((sum, sleeve) => sum + sleeve.cash, 0));
  readonly hasHoldings = computed(() => this.sleeves().some((sleeve) => sleeve.positions > 0 || sleeve.cash !== 0));
  readonly reports = computed(() => this.reportsResource.value() ?? []);
  readonly openReport = computed(() => this.reports().find((report) => report.kind !== 'RiskReview' && (report.status === 'New' || report.status === 'InProgress')));
  readonly latestRisk = computed(() => this.reports().find((report) => report.kind === 'RiskReview'));
  readonly hasStrategyAssigned = computed(() => this.account()?.subPortfolios.some((sub) => sub.status === 'Active' && sub.strategyId) ?? false);

  readonly allocation = computed(() => {
    const total = this.totalValue();
    const targets = new Map(this.account()?.subPortfolios.map((sub) => [sub.id, sub.targetShare]) ?? []);
    return this.sleeves().map((sleeve) => ({ name: sleeve.name, actual: total ? sleeve.value / total : 0, target: targets.get(sleeve.id) ?? 0, value: sleeve.value }));
  });
}
