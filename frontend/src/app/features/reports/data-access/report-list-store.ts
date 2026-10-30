import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { OpenActionsBadge } from '../../../core/notifications/open-actions-badge';
import { distinctReports, ScheduledReport } from '../models/report-schedule';
import { ReportsApi } from './reports-api';
import { toSummaryVm } from './report-mapper';

/** Reports of the current account and "generate now" (D14) for each report its strategies declare. */
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

  private readonly scheduleResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.schedule(accountId),
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
  /** The reports the account's strategies declare, one per code: the "generate" buttons. */
  readonly definitions = computed(() => distinctReports(this.scheduleResource.value()?.reports ?? []));
  readonly definitionsLoaded = computed(() => this.scheduleResource.hasValue());
  /** Code of the report being generated. */
  readonly generating = signal<string | null>(null);

  async generate(report: ScheduledReport, contribution: number | null = null): Promise<void> {
    const accountId = this.accounts.currentId();
    if (!accountId) return;
    this.generating.set(report.code);
    try {
      const { reportId } = await this.api.generate(accountId, report.code, contribution);
      this.messages.add({ severity: 'success', summary: `${report.name}: рапорт готовий` });
      this.badge.refresh();
      await this.router.navigate(['/reports', reportId]);
    } catch (error) {
      this.messages.add({ severity: 'error', summary: `Не вдалося згенерувати «${report.name}»`, detail: toProblemMessage(error), life: 8000 });
    } finally {
      this.generating.set(null);
    }
  }
}
