import { computed, effect, inject, Injectable, resource, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { OpenActionsBadge } from '../../../core/notifications/open-actions-badge';
import { ReportRequestDto } from '../models/report-dto';
import { distinctReports, ScheduledReport } from '../models/report-schedule';
import { ReportsApi } from './reports-api';
import { toSummaryVm } from './report-mapper';

/** How often reports being made are checked while the page is open. */
const REQUEST_POLL_MS = 3000;

/**
 * Reports of the current account and "generate now" (D14) for each report its strategies declare. Strategies may
 * think for minutes or hours, so a report asked for shows as «Готується» until it is made; the one the user just
 * asked for opens by itself when it is ready.
 */
@Injectable()
export class ReportListStore {
  private readonly api = inject(ReportsApi);
  private readonly accounts = inject(AccountContext);
  private readonly messages = inject(MessageService);
  private readonly router = inject(Router);
  private readonly badge = inject(OpenActionsBadge);
  private readonly route = inject(ActivatedRoute);

  private readonly reportsResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.list(accountId),
  });

  private readonly scheduleResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.schedule(accountId),
  });

  private readonly requestsResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) => this.api.requests(accountId),
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
  /** Code of the report being asked for (the request itself, not the strategies' thinking). */
  readonly generating = signal<string | null>(null);
  /** Reports being made: «Готується» until their strategies have answered. */
  readonly preparing = computed(() => (this.requestsResource.value() ?? []).filter((request) => request.status === 'Pending'));
  /** Reports that could not be made in the last day, unless a newer one of the same code came after. */
  readonly failed = computed(() =>
    (this.requestsResource.value() ?? []).filter(
      (request) =>
        request.status === 'Failed' &&
        !(this.reportsResource.value() ?? []).some((report) => report.code === request.code && Date.parse(report.generatedAt) > Date.parse(request.requestedAt)) &&
        !this.preparing().some((pending) => pending.code === request.code),
    ),
  );

  /** The request the user just made (here or by "generate again"): its report opens when it is ready. */
  private awaited: string | null = this.route.snapshot.queryParamMap.get('request');
  /** Requests seen pending, to tell when one has finished. */
  private readonly seenPending = new Set<string>();

  constructor() {
    effect((onCleanup) => {
      const requests = this.requestsResource.value();
      if (!requests) return;
      this.followUp(requests);
      if (!requests.some((request) => request.status === 'Pending')) return;
      const timer = setTimeout(() => this.requestsResource.reload(), REQUEST_POLL_MS);
      onCleanup(() => clearTimeout(timer));
    });
  }

  isPreparing(code: string): boolean {
    return this.preparing().some((request) => request.code === code);
  }

  async generate(report: ScheduledReport, contribution: number | null = null): Promise<void> {
    const accountId = this.accounts.currentId();
    if (!accountId) return;
    this.generating.set(report.code);
    try {
      const { requestId } = await this.api.generate(accountId, report.code, contribution);
      this.awaited = requestId;
      this.messages.add({ severity: 'info', summary: `«${report.name}» готується`, detail: 'Стратегії рахують план; рапорт відкриється, щойно буде готовий.' });
      this.requestsResource.reload();
    } catch (error) {
      this.messages.add({ severity: 'error', summary: `Не вдалося згенерувати «${report.name}»`, detail: toProblemMessage(error), life: 8000 });
    } finally {
      this.generating.set(null);
    }
  }

  /** A request that has just finished: the list gets its report, and the one the user waits for opens. */
  private followUp(requests: ReportRequestDto[]): void {
    let finished = false;
    for (const request of requests) {
      if (request.status === 'Pending') {
        this.seenPending.add(request.id);
        continue;
      }
      const awaited = request.id === this.awaited;
      if (!this.seenPending.delete(request.id) && !awaited) continue;
      finished = true;
      if (awaited) this.awaited = null;
      if (request.status === 'Failed') {
        this.messages.add({ severity: 'error', summary: `Не вдалося підготувати «${request.title}»`, detail: request.error ?? undefined, life: 8000 });
      } else if (awaited && request.reportId) {
        this.messages.add({ severity: 'success', summary: `${request.title}: рапорт готовий` });
        void this.router.navigate(['/reports', request.reportId]);
      } else {
        this.messages.add({ severity: 'success', summary: `${request.title}: рапорт готовий` });
      }
    }
    if (finished) {
      this.reportsResource.reload();
      this.badge.refresh();
    }
  }
}
