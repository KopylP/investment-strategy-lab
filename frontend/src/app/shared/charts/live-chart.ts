import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, effect, inject, input, untracked, viewChild } from '@angular/core';
import Chart, { type ChartConfiguration } from 'chart.js/auto';
import { tapAwayTooltip } from './tap-away-tooltip';

Chart.register(tapAwayTooltip);
if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) Chart.defaults.animation = false;

@Component({
  selector: 'app-live-chart',
  template: `<div class="relative" [style.height]="height()"><canvas #canvas></canvas></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LiveChart {
  readonly type = input<ChartConfiguration['type']>('line');
  readonly data = input.required<object>();
  readonly options = input<object>({});
  readonly plugins = input<object[]>([]);
  readonly height = input('22rem');

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;

  constructor() {
    effect(() => {
      const data = this.data();
      const options = this.options();
      const plugins = this.plugins();
      untracked(() => {
        if (!this.chart) {
          this.chart = new Chart(this.canvas().nativeElement, { type: this.type(), data, options, plugins } as ChartConfiguration);
          return;
        }
        this.chart.data = data as ChartConfiguration['data'];
        this.chart.options = options as ChartConfiguration['options'] & object;
        this.chart.config.plugins!.splice(0, Infinity, ...(plugins as ChartConfiguration['plugins'] & object));
        this.chart.update('none');
      });
    });
    inject(DestroyRef).onDestroy(() => this.chart?.destroy());
  }
}
