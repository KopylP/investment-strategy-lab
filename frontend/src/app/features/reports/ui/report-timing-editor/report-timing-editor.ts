import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { frequencyOptions, weekdayOptions } from '../../models/report-schedule';
import { ReportTiming } from '../../models/report-schedule-dto';

@Component({
  selector: 'app-report-timing-editor',
  imports: [TranslocoPipe, FormsModule, InputNumber, InputText, Select],
  template: `
    <div class="grid gap-3" [class]="columns()">
      <div class="flex flex-col gap-1">
        <label class="text-sm font-medium" [attr.for]="id() + '-frequency'">{{
          'reports.timing.howOften' | transloco
        }}</label>
        <p-select
          [inputId]="id() + '-frequency'"
          [options]="frequencies()"
          optionLabel="label"
          optionValue="value"
          [ngModel]="timing().frequency"
          (ngModelChange)="change({ frequency: $event })"
          fluid
        />
      </div>
      @switch (timing().frequency) {
        @case ('hourly') {
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium" [attr.for]="id() + '-minute'">{{
              'reports.timing.minute' | transloco
            }}</label>
            <p-inputnumber
              [inputId]="id() + '-minute'"
              [ngModel]="timing().minute"
              (ngModelChange)="change({ minute: $event })"
              [min]="0"
              [max]="59"
              [showButtons]="true"
              fluid
            />
          </div>
        }
        @case ('weekly') {
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium" [attr.for]="id() + '-weekday'">{{
              'reports.timing.weekday' | transloco
            }}</label>
            <p-select
              [inputId]="id() + '-weekday'"
              [options]="weekdays"
              optionLabel="label"
              optionValue="value"
              [ngModel]="timing().weekday"
              (ngModelChange)="change({ weekday: $event })"
              fluid
            />
          </div>
        }
      }
      @if (timing().frequency === 'monthly' || timing().frequency === 'quarterly') {
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium" [attr.for]="id() + '-day'">
            {{
              timing().frequency === 'quarterly'
                ? ('reports.timing.quarterDay' | transloco)
                : ('reports.timing.monthDay' | transloco)
            }}
          </label>
          <p-inputnumber
            [inputId]="id() + '-day'"
            [ngModel]="timing().day"
            (ngModelChange)="change({ day: $event })"
            [min]="1"
            [max]="31"
            [showButtons]="true"
            fluid
          />
        </div>
      }
      @if (timing().frequency !== 'off' && timing().frequency !== 'hourly') {
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium" [attr.for]="id() + '-time'">{{
            'reports.timing.time' | transloco
          }}</label>
          <input
            pInputText
            type="time"
            [id]="id() + '-time'"
            [ngModel]="timing().time"
            (ngModelChange)="change({ time: $event })"
          />
        </div>
      }
    </div>
    @if (timing().frequency === 'monthly' || timing().frequency === 'quarterly') {
      <div class="text-xs text-muted-color mt-2">
        {{ 'reports.timing.shortMonthHint' | transloco }}
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportTimingEditor {
  readonly id = input.required<string>();
  readonly timing = model.required<ReportTiming>();

  readonly frequencies = input(frequencyOptions);
  readonly columns = input('sm:grid-cols-3');
  protected readonly weekdays = weekdayOptions;

  protected change(part: Partial<ReportTiming>): void {
    this.timing.update((current) => ({ ...current, ...part }));
  }
}
