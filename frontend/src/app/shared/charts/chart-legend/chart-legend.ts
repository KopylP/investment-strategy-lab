import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LegendLine } from '../legend-line';

@Component({
  selector: 'app-chart-legend',
  imports: [TranslocoPipe],
  template: `<div
    class="flex flex-wrap gap-x-4 gap-y-1 text-sm"
    role="group"
    [attr.aria-label]="'common.chartLines' | transloco"
  >
    @for (line of lines(); track line.label) {
      <button
        type="button"
        class="flex items-start gap-2 p-0 bg-transparent border-0 text-left text-color cursor-pointer"
        [class.opacity-50]="!line.shown"
        [class.line-through]="!line.shown"
        [attr.aria-pressed]="line.shown"
        (click)="toggled.emit(line.label)"
      >
        <span
          class="inline-block w-2.5 h-2.5 rounded-full mt-1 shrink-0"
          [style.background]="line.color"
        ></span>
        {{ line.label }}
      </button>
    }
  </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChartLegend {
  readonly lines = input.required<LegendLine[]>();
  readonly toggled = output<string>();
}
