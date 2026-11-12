import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectButton } from 'primeng/selectbutton';
import { ChartGranularity } from './chart-granularity';

@Component({
  selector: 'app-granularity-toggle',
  imports: [FormsModule, SelectButton, TranslocoPipe],
  template: `<p-selectbutton
    [options]="options()"
    optionLabel="label"
    optionValue="value"
    [ngModel]="granularity.step()"
    (ngModelChange)="$event && granularity.choose($event)"
    [allowEmpty]="false"
    [ariaLabel]="'chart.granularity.label' | transloco"
    size="small"
  />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GranularityToggle {
  protected readonly granularity = inject(ChartGranularity);

  protected readonly options = computed(() => [
    { label: translate('chart.granularity.day'), value: 'day' },
    { label: translate('chart.granularity.week'), value: 'week' },
  ]);
}
