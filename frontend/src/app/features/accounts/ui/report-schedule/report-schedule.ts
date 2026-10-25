import { ChangeDetectionStrategy, Component, inject, input, resource } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import { ApiClient } from '../../../../core/api/api-client';
import { DatePipe } from '../../../../shared/pipes/format-pipes';

interface ScheduledReport {
  kind: 'Rebalance' | 'RiskReview';
  frequency: 'Weekly' | 'Monthly' | 'Quarterly';
  next: string;
  strategies: string[];
}

interface ReportSchedule {
  generateOnSchedule: boolean;
  timeZone: string;
  reports: ScheduledReport[];
}

const frequencies: Record<ScheduledReport['frequency'], string> = { Weekly: 'щотижня', Monthly: 'щомісяця, перша неділя', Quarterly: 'щокварталу, перша неділя кварталу' };

/** Which reports the account gets and when: decided by the strategies of its sub-portfolios ("Рапорти" in each strategy). */
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
          @for (report of schedule.reports; track report.kind + report.frequency) {
            <li class="flex flex-wrap items-start justify-between gap-3 border border-surface rounded-border p-3">
              <div>
                <div class="font-medium flex items-center gap-2">
                  <i class="pi" [class.pi-sync]="report.kind === 'Rebalance'" [class.pi-shield]="report.kind === 'RiskReview'"></i>
                  {{ report.kind === 'Rebalance' ? 'Ребаланс' : 'Перевірка ризику' }}
                  <p-tag [value]="label(report.frequency)" severity="secondary" />
                </div>
                <div class="text-sm text-muted-color mt-1">
                  {{ report.kind === 'Rebalance' ? 'Внесок, купівлі й продажі за рішенням стратегії.' : 'Стоп-лоси й сигнали виходу між ребалансами; продаж лише за сигналом.' }}
                </div>
                <div class="text-xs text-muted-color mt-1">{{ report.strategies.join(' · ') }}</div>
              </div>
              <div class="text-right text-sm">
                <div class="text-muted-color">наступний</div>
                <div class="font-semibold">{{ report.next | date }}, {{ report.kind === 'Rebalance' ? '20:00' : '19:00' }}</div>
              </div>
            </li>
          }
        </ul>
      } @else {
        <div class="text-muted-color">Призначте стратегію під-портфелю — розклад рапортів задає стратегія.</div>
      }
      <div class="text-xs text-muted-color mt-3">
        Частоту задає стратегія (розділ «Рапорти» в її параметрах), час — {{ schedule.timeZone }}. <a routerLink="/strategies" class="text-primary">Стратегії →</a>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportScheduleCard {
  private readonly api = inject(ApiClient);

  readonly accountId = input.required<string>();

  protected readonly schedule = resource({
    params: () => this.accountId(),
    loader: ({ params: id }) => this.api.get<ReportSchedule>(`/api/rebalancing/accounts/${id}/report-schedule`),
  });

  protected label(frequency: ScheduledReport['frequency']): string {
    return frequencies[frequency];
  }
}
