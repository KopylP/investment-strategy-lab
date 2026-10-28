import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { DisplayCurrency } from '../../../../core/context/display-currency';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { formatAxisNumber, formatDate, formatMoney, formatPeriod, shortenDate } from '../../../../shared/pipes/format';
import { Viewport } from '../../../../shared/ui/viewport';
import { DashboardDto } from '../../data-access/dashboard-api';

const showPlanKey = 'overview.valueChart.showPlan';

/** Value over time against the money paid in, with the same two lines "everything as planned" and report markers. */
@Component({
  selector: 'app-value-chart',
  imports: [FormsModule, LiveChart, ToggleSwitch],
  template: `
    @if (hasPlan()) {
      <label class="flex items-center justify-end gap-2 mb-2 text-sm text-muted-color">
        <p-toggleswitch [ngModel]="showPlan()" (ngModelChange)="setShowPlan($event)" /> якби все за планом
      </label>
    }
    <app-live-chart [data]="data()" [options]="options()" [height]="viewport.phone() ? '15rem' : '22rem'" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ValueChart {
  private readonly theme = inject(ChartTheme);
  private readonly display = inject(DisplayCurrency);
  protected readonly viewport = inject(Viewport);

  /** The chart in the display currency at today's NBP rate (the shape is the account's; only the scale changes). */
  private readonly scale = computed(() => {
    const currency = this.dashboard().currency;
    const target = this.display.code();
    const rate = target ? this.display.rate(currency, target) : null;
    return rate === null ? { rate: 1, currency } : { rate, currency: target! };
  });

  readonly dashboard = input.required<DashboardDto>();

  protected readonly hasPlan = computed(() => this.dashboard().series.some((point) => point.planValue !== null));
  /** The plan lines are on unless switched off here; the choice is remembered on this device. */
  protected readonly showPlan = signal(readShowPlan());

  protected setShowPlan(show: boolean): void {
    this.showPlan.set(show);
    try {
      localStorage.setItem(showPlanKey, String(show));
    } catch {
      /* private mode: the choice is just not remembered */
    }
  }

  protected readonly data = computed(() => {
    const { series, reports } = this.dashboard();
    const { rate } = this.scale();
    const scaled = (value: number | null) => (value === null ? null : value * rate);
    const palette = this.theme.palette();
    const reportDays = new Map(reports.map((report) => [report.date, report]));
    const plan = palette.series[1];
    return {
      labels: series.map((point) => formatDate(point.date)),
      datasets: [
        {
          label: 'Вартість',
          data: series.map((point) => scaled(point.value)),
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
          data: series.map((point) => scaled(point.netContributions)),
          borderColor: palette.neutral,
          borderDash: [6, 4],
          stepped: true,
          pointRadius: 0,
          borderWidth: 1.5,
          fill: false,
        },
        // The plan drawn like the real pair, in its own colour: its value solid, the money it would have paid in dashed.
        ...(this.hasPlan() && this.showPlan()
          ? [
              {
                label: 'Вартість за планом',
                data: series.map((point) => scaled(point.planValue)),
                borderColor: plan,
                pointRadius: 0,
                borderWidth: 2,
                tension: 0.2,
                fill: false,
              },
              {
                label: 'Внесено за планом',
                data: series.map((point) => scaled(point.planNetContributions)),
                borderColor: withAlpha(plan, 0.7),
                borderDash: [2, 3],
                stepped: true,
                pointRadius: 0,
                borderWidth: 1.5,
                fill: false,
              },
            ]
          : []),
      ],
    };
  });

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const { series, reports } = this.dashboard();
    const { currency } = this.scale();
    const reportDays = new Map(reports.map((report) => [formatDate(report.date), report]));
    // A phone keeps the lines and drops the ink: a one-row legend, four unrotated dates, amounts without the currency.
    const phone = this.viewport.phone();
    return {
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: {
          labels: phone
            ? { color: palette.text, usePointStyle: true, boxWidth: 6, boxHeight: 6, padding: 10, font: { size: 11 } }
            : { color: palette.text, usePointStyle: true },
        },
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
      scales: phone
        ? {
            x: {
              ticks: {
                color: palette.muted, maxTicksLimit: 4, maxRotation: 0, font: { size: 10 },
                callback(this: { getLabelForValue: (value: number) => string }, value: number) { return shortenDate(this.getLabelForValue(value)); },
              },
              grid: { display: false },
            },
            y: { ticks: { color: palette.muted, maxTicksLimit: 5, font: { size: 10 }, callback: (value: number) => formatAxisNumber(value) }, grid: { color: palette.grid } },
          }
        : {
            x: { ticks: { color: palette.muted, maxTicksLimit: series.length > 60 ? 8 : 12 }, grid: { display: false } },
            y: { ticks: { color: palette.muted, callback: (value: number) => formatMoney(value, currency, 0) }, grid: { color: palette.grid } },
          },
    };
  });
}

function readShowPlan(): boolean {
  try {
    return localStorage.getItem(showPlanKey) !== 'false';
  } catch {
    return true;
  }
}
