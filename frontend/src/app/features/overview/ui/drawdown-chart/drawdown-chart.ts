import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { dateAxis } from '../../../../shared/charts/date-axis';
import { lineChartBase } from '../../../../shared/charts/line-chart-base';
import { valueAxis } from '../../../../shared/charts/value-axis';
import { formatDate, formatSignedPercent } from '../../../../shared/pipes/format';
import { Viewport } from '../../../../shared/ui/viewport';
import { DashboardDto } from '../../data-access/dashboard-api';

@Component({
  selector: 'app-drawdown-chart',
  imports: [LiveChart],
  template: `<app-live-chart
    [data]="data()"
    [options]="options()"
    [height]="viewport.phone() ? '9rem' : '12rem'"
  />`,
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
      datasets: [
        {
          label: 'Просідання',
          data: series.map((point) => point.drawdown),
          borderColor: palette.loss,
          backgroundColor: withAlpha(palette.loss, 0.2),
          fill: 'origin',
          pointRadius: 0,
          borderWidth: 1,
          tension: 0.2,
        },
      ],
    };
  });

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const phone = this.viewport.phone();

    return {
      ...lineChartBase(),
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: (context: { parsed: { y: number } }) => formatSignedPercent(context.parsed.y),
          },
        },
      },
      scales: {
        x: dateAxis(palette, { phone, maxTicks: phone ? 4 : 8, shortLabels: phone }),
        y: {
          ...valueAxis(palette, {
            phone,
            maxTicks: phone ? 4 : undefined,
            tick: (value) => formatSignedPercent(value, 0),
          }),
          max: 0,
        },
      },
    };
  });
}
