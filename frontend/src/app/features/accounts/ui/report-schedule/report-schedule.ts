import { ChangeDetectionStrategy, Component, inject, input, resource } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import { ApiClient } from '../../../../core/api/api-client';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { frequencyLabel, reportIcon, ReportSchedule } from '../../../reports/models/report-schedule';

/** Which reports the account gets and when: what the strategies of its sub-portfolios declare ("Рапорти" in each strategy's parameters). */
@Component({
  selector: 'app-report-schedule',
  imports: [RouterLink, Message, Tag, DatePipe],
  template: `
    @if (schedule.value(); as schedule) {
      @if (!schedule.generateOnSchedule) {
        <p-message severity="warn" styleClass="mb-3" text="Автоматична генерація вимкнена в плані — рапорти лише вручну." />
      }
      @if (schedule.reports.length) {
        <ul class="list-none m-0 p-0 flex flex-col gap-3">
          @for (report of schedule.reports; track report.code + report.frequency + report.time) {
            <li class="flex flex-wrap items-start justify-between gap-3 border border-surface rounded-border p-3">
              <div>
                <div class="font-medium flex items-center gap-2">
                  <i [class]="icon(report.mode)"></i>
                  {{ report.name }}
                  <p-tag [value]="label(report.frequency)" severity="secondary" />
                </div>
                <div class="text-sm text-muted-color mt-1">{{ report.description }}</div>
                <div class="text-xs text-muted-color mt-1">{{ report.strategies.join(' · ') }}</div>
              </div>
              @if (report.next) {
                <!-- Wrapped under the text on a phone: one line instead of a right-aligned pair. -->
                <div class="text-sm flex gap-1 sm:block sm:text-right">
                  <div class="text-muted-color">наступний</div>
                  <div class="font-semibold">{{ report.next | date }}, {{ report.time }}</div>
                </div>
              }
            </li>
          }
        </ul>
      } @else {
        <div class="text-muted-color">Призначте стратегію під-портфелю — рапорти й розклад задає стратегія.</div>
      }
      <div class="text-xs text-muted-color mt-3">
        Які рапорти бувають, задає тип стратегії, а як часто — розділ «Рапорти» в її параметрах; час — {{ schedule.timeZone }}.
        <a routerLink="/strategies" class="text-primary">Стратегії →</a>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportScheduleCard {
  private readonly api = inject(ApiClient);

  readonly accountId = input.required<string>();

  protected readonly label = frequencyLabel;
  protected readonly icon = reportIcon;

  protected readonly schedule = resource({
    params: () => this.accountId(),
    loader: ({ params: id }) => this.api.get<ReportSchedule>(`/api/rebalancing/accounts/${id}/report-schedule`),
  });
}
