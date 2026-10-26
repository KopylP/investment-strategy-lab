import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { formatDate, formatSignedPercent, shortenDate } from '../../../../shared/pipes/format';
import { Viewport } from '../../../../shared/ui/viewport';
import { DashboardDto } from '../../data-access/dashboard-api';

/** How far below its best the time-weighted index was on each day. */
@Component({
  selector: 'app-drawdown-chart',
  imports: [LiveChart],
  template: `<app-live-chart [data]="data()" [options]="options()" [height]="viewport.phone() ? '9rem' : '12rem'" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DrawdownChart {
  private readonly theme = inject(ChartTheme);
  protected readonly viewport = inject(Viewport);

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
    const phone = this.viewport.phone();
    return {
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: (context: { parsed: { y: number } }) => formatSignedPercent(context.parsed.y) } },
      },
      scales: {
        x: phone
          ? {
              ticks: {
                color: palette.muted, maxTicksLimit: 4, maxRotation: 0, font: { size: 10 },
                callback(this: { getLabelForValue: (value: number) => string }, value: number) { return shortenDate(this.getLabelForValue(value)); },
              },
              grid: { display: false },
            }
          : { ticks: { color: palette.muted, maxTicksLimit: 8 }, grid: { display: false } },
        y: {
          max: 0,
          ticks: { color: palette.muted, maxTicksLimit: phone ? 4 : undefined, font: phone ? { size: 10 } : undefined, callback: (value: number) => formatSignedPercent(value, 0) },
          grid: { color: palette.grid },
        },
      },
    };
  });
}
