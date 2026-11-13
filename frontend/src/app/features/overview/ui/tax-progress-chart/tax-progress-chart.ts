import { translate } from '@jsverse/transloco';
import { TaxRegime } from '../../../../core/context/tax-regime';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { dateAxis } from '../../../../shared/charts/date-axis';
import { lineChartBase } from '../../../../shared/charts/line-chart-base';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { valueAxis } from '../../../../shared/charts/value-axis';
import { formatAxisNumber, formatDate, formatMoney } from '../../../../shared/pipes/format';
import { toIsoDate } from '../../../../shared/models/iso-date';
import { Viewport } from '../../../../shared/ui/viewport';
import { TaxProgress } from '../../models/overview-dto';

const NO_CURRENCY = 'XXX';

@Component({
  selector: 'app-tax-progress-chart',
  imports: [LiveChart],
  template: `<app-live-chart
    [data]="data()"
    [options]="options()"
    [height]="viewport.phone() ? '12rem' : '15rem'"
  />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxProgressChart {
  private readonly theme = inject(ChartTheme);
  private readonly regime = inject(TaxRegime);
  protected readonly viewport = inject(Viewport);

  readonly progress = input.required<TaxProgress>();
  readonly threshold = input<number | null>(null);

  private readonly days = computed(() => {
    const { year, points } = this.progress();
    const today = new Date();
    const end = year < today.getFullYear() ? new Date(year, 11, 31) : today;
    const byDay = new Map(points.map((point) => [point.date, point.taxDue]));
    const days: { date: string; tax: number }[] = [];
    let tax = 0;

    for (const day = new Date(year, 0, 1); day <= end; day.setDate(day.getDate() + 1)) {
      const iso = toIsoDate(day);

      tax = byDay.get(iso) ?? tax;
      days.push({ date: iso, tax });
    }

    return days;
  });

  protected readonly data = computed(() => {
    const palette = this.theme.palette();
    const days = this.days();
    const threshold = this.threshold();

    return {
      labels: days.map((day) => formatDate(day.date)),
      datasets: [
        {
          label: translate('overview.tax.title'),
          data: days.map((day) => day.tax),
          borderColor: aboveThreshold(threshold, palette.tax, palette.loss),
          backgroundColor: aboveThreshold(
            threshold,
            withAlpha(palette.tax, 0.15),
            withAlpha(palette.loss, 0.2),
          ),
          fill: 'origin',
          stepped: true,
          borderWidth: 2,
          pointRadius: 0,
          pointHoverRadius: 4,
        },
      ],
    };
  });

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const currency = this.regime.currency() ?? NO_CURRENCY;
    const phone = this.viewport.phone();

    return {
      ...lineChartBase(),
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: (context: { dataset: { label: string }; parsed: { y: number } }) =>
              `${context.dataset.label}: ${formatMoney(context.parsed.y, currency, 0)}`,
          },
        },
      },
      scales: {
        x: dateAxis(palette, { phone, maxTicks: phone ? 4 : 12, shortLabels: true }),
        y: {
          ...valueAxis(palette, {
            phone: false,
            maxTicks: 5,
            tick: (value) => (phone ? formatAxisNumber(value) : formatMoney(value, currency, 0)),
          }),
          min: 0,
        },
      },
    };
  });
}

interface ChartContext {
  chart: {
    ctx: CanvasRenderingContext2D;
    chartArea?: { top: number; bottom: number };
    scales: { y?: { getPixelForValue: (value: number) => number } };
  };
}

function aboveThreshold(
  threshold: number | null,
  below: string,
  above: string,
): string | ((context: ChartContext) => string | CanvasGradient) {
  if (threshold === null) return below;

  return ({ chart }) => {
    const { ctx, chartArea, scales } = chart;
    if (!chartArea || !scales.y || chartArea.bottom <= chartArea.top) return below;

    const at =
      (chartArea.bottom - scales.y.getPixelForValue(threshold)) /
      (chartArea.bottom - chartArea.top);
    if (at >= 1) return below;

    const stop = Math.max(0, at);
    const gradient = ctx.createLinearGradient(0, chartArea.bottom, 0, chartArea.top);

    gradient.addColorStop(0, below);
    gradient.addColorStop(stop, below);
    gradient.addColorStop(stop, above);
    gradient.addColorStop(1, above);

    return gradient;
  };
}
