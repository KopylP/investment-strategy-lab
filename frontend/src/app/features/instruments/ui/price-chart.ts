import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { UIChart } from 'primeng/chart';
import { ChartTheme, withAlpha } from '../../../shared/charts/chart-theme';
import { formatDate, formatMoney } from '../../../shared/pipes/format';
import { QuotePoint } from '../data-access/instruments-store';

/** Close price over the chosen period, filled under the line in the gain/loss colour of the period. */
@Component({
  selector: 'app-price-chart',
  imports: [UIChart],
  template: `@for (chart of [options()]; track chart) { <p-chart type="line" [data]="data()" [options]="chart" height="18rem" /> }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PriceChart {
  private readonly theme = inject(ChartTheme);

  readonly points = input.required<QuotePoint[]>();
  readonly currency = input.required<string>();

  private readonly color = computed(() => {
    const points = this.points();
    const palette = this.theme.palette();
    return (points.at(-1)?.close ?? 0) >= (points.at(0)?.close ?? 0) ? palette.gain : palette.loss;
  });

  protected readonly data = computed(() => ({
    labels: this.points().map((point) => formatDate(point.date)),
    datasets: [{
      label: 'Ціна закриття',
      data: this.points().map((point) => point.close),
      borderColor: this.color(),
      backgroundColor: withAlpha(this.color(), 0.12),
      fill: true,
      tension: 0.15,
      borderWidth: 2,
      pointRadius: 0,
      pointHoverRadius: 4,
    }],
  }));

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const currency = this.currency();
    return {
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: (context: { parsed: { y: number } }) => formatMoney(context.parsed.y, currency) } },
      },
      scales: {
        x: { ticks: { color: palette.muted, maxTicksLimit: 6, maxRotation: 0 }, grid: { display: false } },
        y: { position: 'right', ticks: { color: palette.muted, callback: (value: number) => formatMoney(value, currency, 0) }, grid: { color: palette.grid } },
      },
    };
  });
}
