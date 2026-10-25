import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { UIChart } from 'primeng/chart';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { formatDate, formatSignedPercent } from '../../../../shared/pipes/format';
import { DashboardDto } from '../../data-access/dashboard-api';

/** How far below its best the time-weighted index was on each day. */
@Component({
  selector: 'app-drawdown-chart',
  imports: [UIChart],
  // A fresh chart per data/theme change: p-chart does not redraw reliably when data and options change together.
  template: `@for (chart of [options()]; track chart) { <p-chart type="line" [data]="data()" [options]="chart" height="12rem" /> }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DrawdownChart {
  private readonly theme = inject(ChartTheme);

  readonly dashboard = input.required<DashboardDto>();

  protected readonly data = computed(() => {
    const palette = this.theme.palette();
    const { series } = this.dashboard();
    return {
      labels: series.map((point) => formatDate(point.date)),
      datasets: [{ label: 'Просідання', data: series.map((point) => point.drawdown), borderColor: palette.loss, backgroundColor: withAlpha(palette.loss, 0.2), fill: 'origin', pointRadius: 0, borderWidth: 1, tension: 0.2 }],
    };
  });

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    return {
      maintainAspectRatio: false,
      animation: false,
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: (context: { parsed: { y: number } }) => formatSignedPercent(context.parsed.y) } },
      },
      scales: {
        x: { ticks: { color: palette.muted, maxTicksLimit: 8 }, grid: { display: false } },
        y: { max: 0, ticks: { color: palette.muted, callback: (value: number) => formatSignedPercent(value, 0) }, grid: { color: palette.grid } },
      },
    };
  });
}
