import {
  afterNextRender,
  computed,
  effect,
  inject,
  Injectable,
  resource,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { OpenActionsBadge } from '../../../core/notifications/open-actions-badge';
import { ReportRequestDto } from '../models/report-dto';
import { distinctReports, ScheduledReport } from '../models/report-schedule';
import { ReportListRow } from '../models/report-vm';
import { ReportsApi } from './reports-api';
import { toRequestVm, toSummaryVm } from './report-mapper';

const REQUEST_POLL_MS = 3000;

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
  readonly monthlyContribution = computed(
    () => this.settingsResource.value()?.monthlyContribution ?? 0,
  );
  readonly openReport = computed(() =>
    this.reports().find(
      (report) =>
        report.kind !== 'RiskReview' && (report.status === 'New' || report.status === 'InProgress'),
    ),
  );
  readonly definitions = computed(() =>
    distinctReports(this.scheduleResource.value()?.reports ?? []),
  );
  readonly definitionsLoaded = computed(() => this.scheduleResource.hasValue());
  readonly schedule = computed(() =>
    this.scheduleResource.hasValue() ? this.scheduleResource.value() : undefined,
  );
  readonly generating = signal<string | null>(null);
  readonly preparing = computed(() =>
    (this.requestsResource.value() ?? []).filter((request) => request.status === 'Pending'),
  );
  readonly failed = computed(() =>
    (this.requestsResource.value() ?? []).filter(
      (request) =>
        request.status === 'Failed' &&
        !(this.reportsResource.value() ?? []).some(
          (report) =>
            report.code === request.code &&
            Date.parse(report.generatedAt) > Date.parse(request.requestedAt),
        ) &&
        !this.preparing().some((pending) => pending.code === request.code),
    ),
  );

  readonly rows = computed<ReportListRow[]>(() => [
    ...[...this.preparing(), ...this.failed()].map((request) => ({
      id: request.id,
      request: toRequestVm(request),
    })),
    ...this.reports().map((report) => ({ id: report.id, report })),
  ]);

  private awaited: string | null = this.route.snapshot.queryParamMap.get('request');
  private readonly seenPending = new Set<string>();

  constructor() {
    if (this.awaited)
      afterNextRender(
        () =>
          void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true,
          }),
      );

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
      this.messages.add({
        severity: 'info',
        summary: `«${report.name}» готується`,
        detail: 'Стратегії рахують план; рапорт відкриється, щойно буде готовий.',
      });
      this.requestsResource.reload();
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: `Не вдалося згенерувати «${report.name}»`,
        detail: toProblemMessage(error),
        life: 8000,
      });
    } finally {
      this.generating.set(null);
    }
  }

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

      this.announce(request, awaited);
    }

    if (finished) {
      this.reportsResource.reload();
      this.scheduleResource.reload();
      this.badge.refresh();
    }
  }

  private announce(request: ReportRequestDto, awaited: boolean): void {
    if (request.status === 'Failed') {
      this.messages.add({
        severity: 'error',
        summary: `Не вдалося підготувати «${request.title}»`,
        detail: request.error ?? undefined,
        life: 8000,
      });

      return;
    }

    this.messages.add({ severity: 'success', summary: `${request.title}: рапорт готовий` });

    if (awaited && request.reportId) void this.router.navigate(['/reports', request.reportId]);
  }
}
