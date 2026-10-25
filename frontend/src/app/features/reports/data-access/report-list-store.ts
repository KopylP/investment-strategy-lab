import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { OpenActionsBadge } from '../../../core/notifications/open-actions-badge';
import { ReportsApi } from './reports-api';
import { toSummaryVm } from './report-mapper';

/** Reports of the current account and the "generate now" commands (D14). */
@Injectable()
export class ReportListStore {
  private readonly api = inject(ReportsApi);
  private readonly accounts = inject(AccountContext);
  private readonly messages = inject(MessageService);
  private readonly router = inject(Router);
  private readonly badge = inject(OpenActionsBadge);

  private readonly reportsResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.list(accountId),
  });

  private readonly settingsResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.settings(accountId),
  });

  readonly account = this.accounts.current;
  readonly hasAccount = computed(() => !!this.accounts.current());
  readonly accountsLoaded = this.accounts.hasLoaded;
  readonly reports = computed(() => (this.reportsResource.value() ?? []).map(toSummaryVm));
  readonly isLoading = this.reportsResource.isLoading;
  readonly loadError = this.reportsResource.error;
  readonly monthlyContribution = computed(() => this.settingsResource.value()?.monthlyContribution ?? 0);
  readonly openReport = computed(() => this.reports().find((report) => report.kind !== 'RiskReview' && (report.status === 'New' || report.status === 'InProgress')));
  readonly generating = signal<'monthly' | 'risk' | null>(null);

  async generateMonthly(contribution: number | null): Promise<void> {
    const accountId = this.accounts.currentId();
    if (!accountId) return;
    this.generating.set('monthly');
    try {
      const { reportId } = await this.api.generateMonthly(accountId, contribution);
      this.messages.add({ severity: 'success', summary: 'Рапорт готовий', detail: 'План на місяць розраховано' });
      this.badge.refresh();
      await this.router.navigate(['/reports', reportId]);
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося згенерувати рапорт', detail: toProblemMessage(error), life: 8000 });
    } finally {
      this.generating.set(null);
    }
  }

  async generateRiskReview(): Promise<void> {
    const accountId = this.accounts.currentId();
    if (!accountId) return;
    this.generating.set('risk');
    try {
      const { reportId } = await this.api.generateRiskReview(accountId);
      this.badge.refresh();
      await this.router.navigate(['/reports', reportId]);
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Не вдалося оцінити ризик', detail: toProblemMessage(error), life: 8000 });
    } finally {
      this.generating.set(null);
    }
  }
}
