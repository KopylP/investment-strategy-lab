import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { nextLabel, reportIcon, ReportSchedule, scheduleLabel, upcomingReports } from '../../models/report-schedule';

/** When to expect the account's next reports, soonest first, each with its schedule. */
@Component({
  selector: 'app-next-reports',
  imports: [RouterLink],
  template: `
    <div class="card">
      <div class="flex flex-wrap items-center justify-between gap-2 mb-3">
        <h2 class="text-xl font-semibold m-0">Наступні рапорти</h2>
        <a [routerLink]="['/accounts', accountId()]" class="text-sm text-primary">Змінити час →</a>
      </div>
      @if (!schedule().generateOnSchedule) {
        <div class="text-muted-color">Автоматична генерація вимкнена в плані — наступні рапорти лише вручну.</div>
      } @else {
        <ul class="list-none m-0 p-0 flex flex-col gap-2">
          @for (report of reports(); track report.code) {
            <li class="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
              <span class="flex items-center gap-2">
                <i [class]="icon(report.mode)"></i>
                {{ report.name }}
              </span>
              @if (report.next) {
                <span class="text-sm"><span class="font-semibold">{{ next(report.next) }}</span><span class="text-muted-color"> · {{ label(report.schedule) }}</span></span>
              } @else {
                <span class="text-sm text-muted-color">лише вручну</span>
              }
            </li>
          }
        </ul>
        <div class="text-xs text-muted-color mt-3">Час — {{ schedule().timeZone }}.</div>
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
