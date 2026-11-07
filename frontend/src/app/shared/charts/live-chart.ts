import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import Chart, { type ChartConfiguration, type TooltipModel } from 'chart.js/auto';
import { Viewport } from '../ui/viewport';
import { ChartReadout, readoutOf } from './chart-readout';
import { scrubLine } from './scrub-line';
import { tapAwayTooltip } from './tap-away-tooltip';

Chart.register(tapAwayTooltip, scrubLine);
if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) Chart.defaults.animation = false;

interface ChartOptionsWithPlugins {
  plugins?: { tooltip?: object };
}

@Component({
  selector: 'app-live-chart',
  template: `
    <div class="relative" [style.height]="height()">
      @if (readout(); as shown) {
        <div class="chart-readout" aria-hidden="true">
          <div class="readout-title">{{ shown.title }}</div>
          @for (line of shown.lines; track $index) {
            <div class="readout-line">
              <span class="readout-dot" [style.background]="line.color"></span>
              <span class="readout-label">{{ line.label }}</span>
              <span class="readout-value">{{ line.value }}</span>
            </div>
          }
        </div>
      }
      <canvas #canvas></canvas>
    </div>
  `,
  styles: `
    .chart-readout {
      position: absolute;
      right: 0;
      bottom: 100%;
      left: 0;
      z-index: 2;
      margin-bottom: 6px;
      padding: 8px 12px;
      border: 1px solid var(--p-content-border-color);
      border-radius: 12px;
      background: var(--p-content-background);
      box-shadow: 0 4px 16px rgb(0 0 0 / 0.08);
      font-size: 13px;
      line-height: 1.45;
      pointer-events: none;
    }
    .readout-title {
      font-weight: 600;
    }
    .readout-line {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .readout-dot {
      width: 8px;
      height: 8px;
      flex-shrink: 0;
      border-radius: 50%;
    }
    .readout-label {
      flex: 1;
      min-width: 0;
      overflow: hidden;
      color: var(--p-text-muted-color);
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .readout-value {
      flex-shrink: 0;
      font-variant-numeric: tabular-nums;
      font-weight: 500;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LiveChart {
  readonly type = input<ChartConfiguration['type']>('line');
  readonly data = input.required<object>();
  readonly options = input<object>({});
  readonly plugins = input<object[]>([]);
  readonly height = input('22rem');

  private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly viewport = inject(Viewport);
  private chart: Chart | null = null;

  protected readonly readout = signal<ChartReadout | null>(null);

  constructor() {
    effect(() => {
      const data = this.data();
      const options = this.optionsForScreen(this.options(), this.viewport.phone());
      const plugins = this.plugins();

      untracked(() => {
        this.readout.set(null);
        if (this.chart) this.updateChart(data, options, plugins);
        else this.createChart(data, options, plugins);
      });
    });
    inject(DestroyRef).onDestroy(() => this.chart?.destroy());
  }

  private optionsForScreen(options: ChartOptionsWithPlugins, phone: boolean): object {
    return phone ? this.withReadoutAboveChart(options) : options;
  }

  private withReadoutAboveChart(options: ChartOptionsWithPlugins): object {
    const tooltip = {
      ...options.plugins?.tooltip,
      enabled: false,
      external: ({ tooltip }: { tooltip: TooltipModel<never> }) =>
        this.readout.set(readoutOf(tooltip)),
    };

    return { ...options, plugins: { ...options.plugins, tooltip } };
  }

  private createChart(data: unknown, options: unknown, plugins: unknown): void {
    this.chart = new Chart(this.canvas().nativeElement, {
      type: this.type(),
      data,
      options,
      plugins,
    } as ChartConfiguration);
  }

  private updateChart(data: unknown, options: unknown, plugins: unknown): void {
    const chart = this.chart!;

    chart.data = data as ChartConfiguration['data'];
    chart.options = options as ChartConfiguration['options'] & object;
    chart.config.plugins!.splice(
      0,
      Infinity,
      ...(plugins as ChartConfiguration['plugins'] & object),
    );
    chart.update('none');
  }
}
