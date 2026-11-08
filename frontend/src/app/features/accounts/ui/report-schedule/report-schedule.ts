import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject, input, resource, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import { ApiClient, toProblemMessage } from '../../../../core/api/api-client';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import {
  editableTiming,
  isTimingComplete,
  reportIcon,
  ReportSchedule,
  ReportTiming,
  ScheduledReport,
  scheduleLabel,
} from '../../../reports/models/report-schedule';
import { ReportTimingEditor } from '../../../reports/ui/report-timing-editor/report-timing-editor';

@Component({
  selector: 'app-report-schedule',
  imports: [TranslocoPipe, RouterLink, Button, Message, Tag, DatePipe, ReportTimingEditor],
  template: `
    @if (schedule.value(); as schedule) {
      @if (!schedule.generateOnSchedule) {
        <p-message
          severity="warn"
          styleClass="mb-3"
          [text]="'accounts.schedule.autoOff' | transloco"
        />
      }
      @if (schedule.reports.length) {
        <ul class="list-none m-0 p-0 flex flex-col gap-3">
          @for (report of schedule.reports; track report.code + label(report.schedule)) {
            <li class="border border-surface rounded-border p-3">
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <div class="font-medium flex flex-wrap items-center gap-2">
                    <i [class]="icon(report.mode)"></i>
                    {{ report.name }}
                    <p-tag
                      [value]="label(report.schedule)"
                      [severity]="report.own ? 'info' : 'secondary'"
                    />
                  </div>
                  <div class="text-sm text-muted-color mt-1">{{ report.description }}</div>
                  <div class="text-xs text-muted-color mt-1">
                    {{
                      report.own
                        ? ('accounts.schedule.ownTime'
                          | transloco: { strategy: label(report.strategySchedule) })
                        : ('accounts.schedule.strategyTime' | transloco)
                    }}
                    · {{ report.strategies.join(' · ') }}
                  </div>
                </div>
                <div class="text-sm flex flex-wrap items-center gap-2 sm:flex-col sm:items-end">
                  @if (report.next) {
                    <div class="flex gap-1 sm:block sm:text-right">
                      <div class="text-muted-color">{{ 'accounts.schedule.next' | transloco }}</div>
                      <div class="font-semibold">
                        {{ report.next.slice(0, 10) | date }}, {{ report.next.slice(11) }}
                      </div>
                    </div>
                  }
                  @if (editing() !== report.code) {
                    <p-button
                      [label]="'accounts.schedule.changeTime' | transloco"
                      icon="pi pi-clock"
                      size="small"
                      [text]="true"
                      (onClick)="edit(report)"
                    />
                  }
                </div>
              </div>
              @if (editing() === report.code) {
                <div class="mt-3 pt-3 border-t border-surface flex flex-col gap-3">
                  <app-report-timing-editor [id]="report.code" [(timing)]="draft" />
                  <div class="flex flex-wrap justify-end gap-2">
                    @if (report.own) {
                      <p-button
                        [label]="'accounts.asInStrategyLabel' | transloco"
                        severity="secondary"
                        [text]="true"
                        [disabled]="saving()"
                        (onClick)="reset(report)"
                      />
                    }
                    <p-button
                      [label]="'common.cancel' | transloco"
                      severity="secondary"
                      [text]="true"
                      [disabled]="saving()"
                      (onClick)="editing.set(null)"
                    />
                    <p-button
                      [label]="'common.save' | transloco"
                      icon="pi pi-check"
                      [loading]="saving()"
                      [disabled]="!complete(draft())"
                      (onClick)="save(report)"
                    />
                  </div>
                </div>
              }
            </li>
          }
        </ul>
      } @else {
        <div class="text-muted-color">
          {{ 'accounts.schedule.assignStrategy' | transloco }}
        </div>
      }
      <div class="text-xs text-muted-color mt-3">
        {{ 'accounts.schedule.hint' | transloco: { timeZone: schedule.timeZone } }}
        <a routerLink="/strategies" class="text-primary">{{
          'accounts.schedule.strategiesLink' | transloco
        }}</a>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportScheduleCard {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);

  readonly accountId = input.required<string>();

  protected readonly label = scheduleLabel;
  protected readonly icon = reportIcon;
  protected readonly complete = isTimingComplete;

  protected readonly editing = signal<string | null>(null);
  protected readonly draft = signal<ReportTiming>({
    frequency: 'monthly',
    day: 1,
    weekday: 'sunday',
    time: '20:00',
    minute: 0,
  });
  protected readonly saving = signal(false);

  protected readonly schedule = resource({
    params: () => this.accountId(),
    loader: ({ params: id }) => this.api.get<ReportSchedule>(this.url(id)),
  });

  protected edit(report: ScheduledReport): void {
    this.draft.set(editableTiming(report.schedule));
    this.editing.set(report.code);
  }

  protected save(report: ScheduledReport): Promise<void> {
    return this.apply(
      () =>
        this.api.put<ReportSchedule>(`${this.url(this.accountId())}/${report.code}`, this.draft()),
      `${report.name}: ${scheduleLabel(this.draft())}`,
    );
  }

  protected reset(report: ScheduledReport): Promise<void> {
    return this.apply(
      () => this.api.delete<ReportSchedule>(`${this.url(this.accountId())}/${report.code}`),
      translate('accounts.schedule.resetDone', { report: report.name }),
    );
  }

  private async apply(request: () => Promise<ReportSchedule>, summary: string): Promise<void> {
    this.saving.set(true);

    try {
      this.schedule.set(await request());
      this.editing.set(null);
      this.messages.add({ severity: 'success', summary });
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: translate('accounts.schedule.notChanged'),
        detail: toProblemMessage(error),
      });
    } finally {
      this.saving.set(false);
    }
  }

  private url(accountId: string): string {
    return `/api/rebalancing/accounts/${accountId}/report-schedule`;
  }
}
