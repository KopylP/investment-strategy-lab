import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { formatAxisNumber, formatDate, formatMoney, shortenDate } from '../../../../shared/pipes/format';
import { toIsoDate } from '../../../../shared/models/api';
import { Viewport } from '../../../../shared/ui/viewport';
import { TaxProgress } from '../../data-access/overview-store';

/**
 * The year's PIT-38 tax day by day since 1 January (a step on every day with a sale or a dividend); above the threshold,
 * when there is one, the line and its fill turn red. The threshold only colours: the axis keeps following the tax.
 */
@Component({
  selector: 'app-tax-progress-chart',
  imports: [LiveChart],
  template: `<app-live-chart [data]="data()" [options]="options()" [height]="viewport.phone() ? '12rem' : '15rem'" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxProgressChart {
  private readonly theme = inject(ChartTheme);
  protected readonly viewport = inject(Viewport);

  readonly progress = input.required<TaxProgress>();
  /** The threshold in PLN (share × all accounts' value); null — none. */
  readonly threshold = input<number | null>(null);

  /** Every day from 1 January to today (or 31 December of a past year), carrying the last known tax. */
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
          label: 'Податок',
          data: days.map((day) => day.tax),
          borderColor: aboveThreshold(threshold, palette.tax, palette.loss),
          backgroundColor: aboveThreshold(threshold, withAlpha(palette.tax, 0.15), withAlpha(palette.loss, 0.2)),
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
    const phone = this.viewport.phone();
    return {
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: { label: (context: { dataset: { label: string }; parsed: { y: number } }) => `${context.dataset.label}: ${formatMoney(context.parsed.y, 'PLN', 0)}` },
        },
      },
      scales: {
        x: {
          ticks: {
            color: palette.muted, maxTicksLimit: phone ? 4 : 12, maxRotation: 0, font: phone ? { size: 10 } : undefined,
            callback(this: { getLabelForValue: (value: number) => string }, value: number) { return shortenDate(this.getLabelForValue(value)); },
          },
          grid: { display: false },
        },
        y: {
          min: 0,
          ticks: { color: palette.muted, maxTicksLimit: 5, callback: (value: number) => (phone ? formatAxisNumber(value) : formatMoney(value, 'PLN', 0)) },
          grid: { color: palette.grid },
        },
      },
    };
  });
}

interface ChartContext {
  chart: { ctx: CanvasRenderingContext2D; chartArea?: { top: number; bottom: number }; scales: { y?: { getPixelForValue: (value: number) => number } } };
}

/** A colour that switches at the threshold's height on the chart (a vertical gradient with a hard stop); null — always below. */
function aboveThreshold(threshold: number | null, below: string, above: string): string | ((context: ChartContext) => string | CanvasGradient) {
  if (threshold === null) return below;
  return ({ chart }) => {
    const { ctx, chartArea, scales } = chart;
    if (!chartArea || !scales.y || chartArea.bottom <= chartArea.top) return below;
    const at = (chartArea.bottom - scales.y.getPixelForValue(threshold)) / (chartArea.bottom - chartArea.top);
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
