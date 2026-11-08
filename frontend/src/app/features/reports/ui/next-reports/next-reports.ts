import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  nextLabel,
  reportIcon,
  ReportSchedule,
  scheduleLabel,
  upcomingReports,
} from '../../models/report-schedule';

@Component({
  selector: 'app-next-reports',
  imports: [TranslocoPipe, RouterLink],
  template: `
    <div class="text-sm text-surface-600 dark:text-muted-color">
      <div class="flex items-baseline justify-between gap-3">
        <span>{{ 'reports.next.title' | transloco }}</span>
        <a
          [routerLink]="['/accounts', accountId()]"
          class="text-primary no-underline whitespace-nowrap"
          >{{ 'reports.next.changeTime' | transloco }}</a
        >
      </div>
      @if (!schedule().generateOnSchedule) {
        <div class="mt-1">{{ 'reports.next.manualOnlyHint' | transloco }}</div>
      } @else {
        <ul class="list-none m-0 mt-1 p-0 flex flex-wrap gap-x-6 gap-y-1">
          @for (report of reports(); track report.code) {
            <li
              class="flex items-center gap-2"
              [title]="label(report.schedule) + ' · ' + schedule().timeZone"
            >
              <i [class]="icon(report.mode) + ' text-xs'"></i>
              <span
                >{{ report.name }}:
                <span class="font-medium text-color">{{
                  report.next ? next(report.next) : ('reports.schedule.manualLower' | transloco)
                }}</span></span
              >
            </li>
          }
        </ul>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NextReports {
  readonly schedule = input.required<ReportSchedule>();
  readonly accountId = input.required<string>();

  protected readonly reports = computed(() => upcomingReports(this.schedule().reports));
  protected readonly next = nextLabel;
  protected readonly label = scheduleLabel;
  protected readonly icon = reportIcon;
}
