import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, effect, inject, input, untracked, viewChild } from '@angular/core';
import Chart, { type ChartConfiguration } from 'chart.js/auto';
import { tapAwayTooltip } from './tap-away-tooltip';

// Every chart of the app, p-chart's too: they share the one Chart.js registry.
Chart.register(tapAwayTooltip);

/**
 * A Chart.js chart created once and updated in place: new data or options swap into the same canvas, so changing a
 * period or an instrument does not blink the way re-creating p-chart does.
 */
@Component({
  selector: 'app-live-chart',
  template: `<div class="relative" [style.height]="height()"><canvas #canvas></canvas></div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LiveChart {
  // Loosely typed like p-chart's inputs: the callers build plain Chart.js objects with their own narrower callback types.
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
        // The config's plugin list has no setter; update() re-reads it, so it is swapped in place.
        this.chart.config.plugins!.splice(0, Infinity, ...(plugins as ChartConfiguration['plugins'] & object));
        this.chart.update('none');
      });
    });
    inject(DestroyRef).onDestroy(() => this.chart?.destroy());
  }
}
