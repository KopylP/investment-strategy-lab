import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { UIChart } from 'primeng/chart';
import { ChartTheme } from '../../../../shared/charts/chart-theme';
import { formatDate, formatMoney, formatPeriod } from '../../../../shared/pipes/format';
import { DashboardDto } from '../../data-access/dashboard-api';

/** Value over time against the money paid in, with the "everything as planned" line and report markers. */
@Component({
  selector: 'app-value-chart',
  imports: [UIChart],
  // A fresh chart per data/theme change: p-chart does not redraw reliably when data and options change together.
  template: `@for (chart of [options()]; track chart) { <p-chart type="line" [data]="data()" [options]="chart" height="22rem" /> }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ValueChart {
  private readonly theme = inject(ChartTheme);

  readonly dashboard = input.required<DashboardDto>();

  protected readonly data = computed(() => {
    const { series, reports } = this.dashboard();
    const palette = this.theme.palette();
    const reportDays = new Map(reports.map((report) => [report.date, report]));
    const hasPlan = series.some((point) => point.planValue !== null);
    return {
      labels: series.map((point) => formatDate(point.date)),
      datasets: [
        {
          label: 'Вартість',
          data: series.map((point) => point.value),
          borderColor: palette.primary,
          backgroundColor: palette.primarySoft,
          fill: false,
          tension: 0.2,
          borderWidth: 2,
          pointRadius: series.map((point) => (reportDays.has(point.date) ? 5 : 0)),
          pointBackgroundColor: palette.tax,
          pointHoverRadius: 6,
        },
        {
          label: 'Внесено',
          data: series.map((point) => point.netContributions),
          borderColor: palette.neutral,
          borderDash: [6, 4],
          stepped: true,
          pointRadius: 0,
          borderWidth: 1.5,
          fill: false,
        },
        ...(hasPlan
          ? [{
              label: 'Якби все за планом',
              data: series.map((point) => point.planValue),
              borderColor: palette.series[1],
              pointRadius: 0,
              borderWidth: 1.5,
              tension: 0.2,
              fill: false,
            }]
          : []),
      ],
    };
  });

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const { currency, series, reports } = this.dashboard();
    const reportDays = new Map(reports.map((report) => [formatDate(report.date), report]));
    return {
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { labels: { color: palette.text, usePointStyle: true } },
        tooltip: {
          callbacks: {
            label: (context: { dataset: { label: string }; parsed: { y: number } }) => `${context.dataset.label}: ${formatMoney(context.parsed.y, currency, 0)}`,
            afterBody: (items: { label: string }[]) => {
              const report = reportDays.get(items[0]?.label);
              return report ? [`Рапорт: ${report.kind === 'RiskReview' ? 'ризик-рев’ю' : formatPeriod(report.period)}`] : [];
            },
          },
        },
      },
      scales: {
        x: { ticks: { color: palette.muted, maxTicksLimit: series.length > 60 ? 8 : 12 }, grid: { display: false } },
        y: { ticks: { color: palette.muted, callback: (value: number) => formatMoney(value, currency, 0) }, grid: { color: palette.grid } },
      },
    };
  });
}
