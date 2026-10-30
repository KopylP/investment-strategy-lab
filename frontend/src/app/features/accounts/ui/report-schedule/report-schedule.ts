import { ChangeDetectionStrategy, Component, inject, input, resource, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { ApiClient, toProblemMessage } from '../../../../core/api/api-client';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import {
  frequencyOptions, reportIcon, ReportSchedule, ReportTiming, ScheduledReport, scheduleLabel, weekdayOptions,
} from '../../../reports/models/report-schedule';

/**
 * Which reports the account gets and when. The strategies of its sub-portfolios declare the reports and a default time;
 * the user sets their own time for the account (a rebalance on payday) or goes back to the strategy's.
 */
@Component({
  selector: 'app-report-schedule',
  imports: [FormsModule, RouterLink, Button, InputNumber, InputText, Message, Select, Tag, DatePipe],
  template: `
    @if (schedule.value(); as schedule) {
      @if (!schedule.generateOnSchedule) {
        <p-message severity="warn" styleClass="mb-3" text="Автоматична генерація вимкнена в плані — рапорти лише вручну." />
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
                    <p-tag [value]="label(report.schedule)" [severity]="report.own ? 'info' : 'secondary'" />
                  </div>
                  <div class="text-sm text-muted-color mt-1">{{ report.description }}</div>
                  <div class="text-xs text-muted-color mt-1">
                    {{ report.own ? 'Ваш час · стратегія: ' + label(report.strategySchedule) : 'Час стратегії' }} · {{ report.strategies.join(' · ') }}
                  </div>
                </div>
                <!-- Wrapped under the text on a phone: one line instead of a right-aligned pair. -->
                <div class="text-sm flex flex-wrap items-center gap-2 sm:flex-col sm:items-end">
                  @if (report.next) {
                    <div class="flex gap-1 sm:block sm:text-right">
                      <div class="text-muted-color">наступний</div>
                      <div class="font-semibold">{{ report.next.slice(0, 10) | date }}, {{ report.next.slice(11) }}</div>
                    </div>
                  }
                  @if (editing() !== report.code) {
                    <p-button label="Змінити час" icon="pi pi-clock" size="small" [text]="true" (onClick)="edit(report)" />
                  }
                </div>
              </div>
              @if (editing() === report.code) {
                <div class="mt-3 pt-3 border-t border-surface flex flex-col gap-3">
                  <div class="grid gap-3 sm:grid-cols-3">
                    <div class="flex flex-col gap-1">
                      <label class="text-sm font-medium" [attr.for]="report.code + '-frequency'">Як часто</label>
                      <p-select [inputId]="report.code + '-frequency'" [options]="frequencies" optionLabel="label" optionValue="value"
                                [ngModel]="draft().frequency" (ngModelChange)="change({ frequency: $event })" fluid />
                    </div>
                    @switch (draft().frequency) {
                      @case ('hourly') {
                        <div class="flex flex-col gap-1">
                          <label class="text-sm font-medium" [attr.for]="report.code + '-minute'">Хвилина</label>
                          <p-inputnumber [inputId]="report.code + '-minute'" [ngModel]="draft().minute" (ngModelChange)="change({ minute: $event })"
                                         [min]="0" [max]="59" [showButtons]="true" fluid />
                        </div>
                      }
                      @case ('weekly') {
                        <div class="flex flex-col gap-1">
                          <label class="text-sm font-medium" [attr.for]="report.code + '-weekday'">День тижня</label>
                          <p-select [inputId]="report.code + '-weekday'" [options]="weekdays" optionLabel="label" optionValue="value"
                                    [ngModel]="draft().weekday" (ngModelChange)="change({ weekday: $event })" fluid />
                        </div>
                      }
                    }
                    @if (draft().frequency === 'monthly' || draft().frequency === 'quarterly') {
                      <div class="flex flex-col gap-1">
                        <label class="text-sm font-medium" [attr.for]="report.code + '-day'">
                          {{ draft().frequency === 'quarterly' ? 'День першого місяця кварталу' : 'День місяця' }}
                        </label>
                        <p-inputnumber [inputId]="report.code + '-day'" [ngModel]="draft().day" (ngModelChange)="change({ day: $event })"
                                       [min]="1" [max]="31" [showButtons]="true" fluid />
                      </div>
                    }
                    @if (draft().frequency !== 'off' && draft().frequency !== 'hourly') {
                      <div class="flex flex-col gap-1">
                        <label class="text-sm font-medium" [attr.for]="report.code + '-time'">Час</label>
                        <input pInputText type="time" [id]="report.code + '-time'" [ngModel]="draft().time" (ngModelChange)="change({ time: $event })" />
                      </div>
                    }
                  </div>
                  @if (draft().frequency === 'monthly' || draft().frequency === 'quarterly') {
                    <div class="text-xs text-muted-color">У коротшому місяці — його останній день (31-го → 30 листопада).</div>
                  }
                  <div class="flex flex-wrap justify-end gap-2">
                    @if (report.own) {
                      <p-button label="Як у стратегії" severity="secondary" [text]="true" [disabled]="saving()" (onClick)="reset(report)" />
                    }
                    <p-button label="Скасувати" severity="secondary" [text]="true" [disabled]="saving()" (onClick)="editing.set(null)" />
                    <p-button label="Зберегти" icon="pi pi-check" [loading]="saving()" (onClick)="save(report)" />
                  </div>
                </div>
              }
            </li>
          }
        </ul>
      } @else {
        <div class="text-muted-color">Призначте стратегію під-портфелю — рапорти задає стратегія.</div>
      }
      <div class="text-xs text-muted-color mt-3">
        Які рапорти бувають, задає стратегія; коли — вирішуєте ви для цього рахунку, інакше діє час стратегії. Час — {{ schedule.timeZone }}.
        <a routerLink="/strategies" class="text-primary">Стратегії →</a>
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
  protected readonly frequencies = frequencyOptions;
  protected readonly weekdays = weekdayOptions;

  /** The code of the report whose time is being changed. */
  protected readonly editing = signal<string | null>(null);
  protected readonly draft = signal<ReportTiming>({ frequency: 'monthly', day: 1, weekday: 'sunday', time: '20:00', minute: 0 });
  protected readonly saving = signal(false);

  protected readonly schedule = resource({
    params: () => this.accountId(),
    loader: ({ params: id }) => this.api.get<ReportSchedule>(this.url(id)),
  });

  protected edit(report: ScheduledReport): void {
    const { frequency, day, weekday, time, minute } = report.schedule;
    this.draft.set({ frequency: frequency.toLowerCase(), day, weekday: weekday.toLowerCase(), time, minute });
    this.editing.set(report.code);
  }

  protected change(part: Partial<ReportTiming>): void {
    this.draft.update((current) => ({ ...current, ...part }));
  }

  protected save(report: ScheduledReport): Promise<void> {
    return this.apply(() => this.api.put<ReportSchedule>(`${this.url(this.accountId())}/${report.code}`, this.draft()), `${report.name}: ${scheduleLabel(this.draft())}`);
  }

  protected reset(report: ScheduledReport): Promise<void> {
    return this.apply(() => this.api.delete<ReportSchedule>(`${this.url(this.accountId())}/${report.code}`), `${report.name}: час стратегії`);
  }

  private async apply(request: () => Promise<ReportSchedule>, summary: string): Promise<void> {
    this.saving.set(true);
    try {
      this.schedule.set(await request());
      this.editing.set(null);
      this.messages.add({ severity: 'success', summary });
    } catch (error) {
      this.messages.add({ severity: 'error', summary: 'Розклад не змінено', detail: toProblemMessage(error) });
    } finally {
      this.saving.set(false);
    }
  }

  private url(accountId: string): string {
    return `/api/rebalancing/accounts/${accountId}/report-schedule`;
  }
}
