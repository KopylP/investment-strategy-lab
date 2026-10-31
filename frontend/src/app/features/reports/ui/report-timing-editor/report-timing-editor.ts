import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { frequencyOptions, ReportTiming, weekdayOptions } from '../../models/report-schedule';

/** When one report comes: the frequency, then only what it reads — the minute, the time, the weekday or the day of the month. */
@Component({
  selector: 'app-report-timing-editor',
  imports: [FormsModule, InputNumber, InputText, Select],
  template: `
    <div class="grid gap-3" [class]="columns()">
      <div class="flex flex-col gap-1">
        <label class="text-sm font-medium" [attr.for]="id() + '-frequency'">Як часто</label>
        <p-select [inputId]="id() + '-frequency'" [options]="frequencies()" optionLabel="label" optionValue="value"
                  [ngModel]="timing().frequency" (ngModelChange)="change({ frequency: $event })" fluid />
      </div>
      @switch (timing().frequency) {
        @case ('hourly') {
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium" [attr.for]="id() + '-minute'">Хвилина</label>
            <p-inputnumber [inputId]="id() + '-minute'" [ngModel]="timing().minute" (ngModelChange)="change({ minute: $event })"
                           [min]="0" [max]="59" [showButtons]="true" fluid />
          </div>
        }
        @case ('weekly') {
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium" [attr.for]="id() + '-weekday'">День тижня</label>
            <p-select [inputId]="id() + '-weekday'" [options]="weekdays" optionLabel="label" optionValue="value"
                      [ngModel]="timing().weekday" (ngModelChange)="change({ weekday: $event })" fluid />
          </div>
        }
      }
      @if (timing().frequency === 'monthly' || timing().frequency === 'quarterly') {
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium" [attr.for]="id() + '-day'">
            {{ timing().frequency === 'quarterly' ? 'День першого місяця кварталу' : 'День місяця' }}
          </label>
          <p-inputnumber [inputId]="id() + '-day'" [ngModel]="timing().day" (ngModelChange)="change({ day: $event })"
                         [min]="1" [max]="31" [showButtons]="true" fluid />
        </div>
      }
      @if (timing().frequency !== 'off' && timing().frequency !== 'hourly') {
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium" [attr.for]="id() + '-time'">Час</label>
          <input pInputText type="time" [id]="id() + '-time'" [ngModel]="timing().time" (ngModelChange)="change({ time: $event })" />
        </div>
      }
    </div>
    @if (timing().frequency === 'monthly' || timing().frequency === 'quarterly') {
      <div class="text-xs text-muted-color mt-2">У коротшому місяці — його останній день (31-го → 30 листопада).</div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportTimingEditor {
  /** Prefix of the fields' ids, unique on the page (the report's code). */
  readonly id = input.required<string>();
  /** Lower-case frequency and weekday, as the API takes them. */
  readonly timing = model.required<ReportTiming>();

  readonly frequencies = input(frequencyOptions);
  readonly columns = input('sm:grid-cols-3');
  protected readonly weekdays = weekdayOptions;

  protected change(part: Partial<ReportTiming>): void {
    this.timing.update((current) => ({ ...current, ...part }));
  }
}
