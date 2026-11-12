import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { DisplayCurrency } from '../../../../core/context/display-currency';
import { ChartGranularity } from '../../../../shared/charts/chart-granularity';
import { ChartLegend } from '../../../../shared/charts/chart-legend/chart-legend';
import { ChartTheme, withAlpha } from '../../../../shared/charts/chart-theme';
import { dateAxis } from '../../../../shared/charts/date-axis';
import { GranularityToggle } from '../../../../shared/charts/granularity-toggle';
import { HiddenLines } from '../../../../shared/charts/hidden-lines';
import { lineChartBase } from '../../../../shared/charts/line-chart-base';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { bucketDatesByDate, bucketsOf } from '../../../../shared/charts/series-buckets';
import { valueAxis } from '../../../../shared/charts/value-axis';
import {
  formatAxisNumber,
  formatDate,
  formatMoney,
  formatPeriod,
} from '../../../../shared/pipes/format';
import { Viewport } from '../../../../shared/ui/viewport';
import { DashboardDto } from '../../data-access/dashboard-api';

const showPlanKey = 'overview.valueChart.showPlan';
const showHeldKey = 'overview.valueChart.showHeld';
const phoneMaxTicks = 4;

@Component({
  selector: 'app-value-chart',
  imports: [TranslocoPipe, FormsModule, ChartLegend, GranularityToggle, LiveChart, ToggleSwitch],
  template: ` <div
      class="flex flex-wrap items-center justify-end gap-x-4 gap-y-2 mb-2 text-sm text-muted-color"
    >
      <app-granularity-toggle />
      @if (hasPlan() && !showHeld()) {
        <label class="flex items-center gap-2">
          <p-toggleswitch [ngModel]="showPlan()" (ngModelChange)="setShowPlan($event)" />
          {{ 'overview.chart.ifToPlan' | transloco }}
        </label>
      }
      <label class="flex items-center gap-2">
        <p-toggleswitch [ngModel]="showHeld()" (ngModelChange)="setShowHeld($event)" />
        {{ 'overview.chart.gainOnAssets' | transloco }}
      </label>
    </div>
    <app-chart-legend
      class="block mb-2"
      [lines]="legend()"
      (toggled)="hiddenLines.toggle($event)"
    />
    <app-live-chart
      [data]="data()"
      [options]="options()"
      [height]="viewport.phone() ? '15rem' : '22rem'"
    />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ValueChart {
  private readonly theme = inject(ChartTheme);
  private readonly display = inject(DisplayCurrency);
  private readonly granularity = inject(ChartGranularity);
  protected readonly viewport = inject(Viewport);

  private readonly scale = computed(() => {
    const currency = this.dashboard().currency;
    const target = this.display.code();
    const rate = target ? this.display.rate(currency, target) : null;

    return rate === null ? { rate: 1, currency } : { rate, currency: target! };
  });

  readonly dashboard = input.required<DashboardDto>();

  protected readonly hasPlan = computed(() =>
    this.dashboard().series.some((point) => point.planValue !== null),
  );
  protected readonly showPlan = signal(readChoice(showPlanKey, true));
  protected readonly showHeld = signal(readChoice(showHeldKey, false));

  protected setShowPlan(show: boolean): void {
    this.showPlan.set(show);
    remember(showPlanKey, show);
  }

  protected setShowHeld(show: boolean): void {
    this.showHeld.set(show);
    remember(showHeldKey, show);
  }

  private readonly buckets = computed(() =>
    bucketsOf(this.dashboard().series, this.granularity.step()),
  );

  private readonly reportsByBucketDate = computed(() => {
    const bucketDates = bucketDatesByDate(this.buckets());
    const byBucketDate = new Map<string, DashboardDto['reports'][number]>();

    for (const report of this.dashboard().reports) {
      const bucketDate = bucketDates.get(report.date);

      if (bucketDate) byBucketDate.set(bucketDate, report);
    }

    return byBucketDate;
  });

  private readonly allLines = computed(() => {
    const buckets = this.buckets();
    const points = buckets.map((bucket) => bucket.last);
    const { rate } = this.scale();
    const scaled = (value: number | null) => (value === null ? null : value * rate);
    const palette = this.theme.palette();
    const reportDates = this.reportsByBucketDate();
    const plan = palette.series[1];
    const held = this.showHeld();
    const valueLabel = held
      ? translate('overview.chart.assetsValue')
      : translate('overview.chart.value');
    return {
      labels: buckets.map((bucket) => formatDate(bucket.date)),
      datasets: [
        {
          type: 'line' as const,
          label: valueLabel,
          data: points.map((point) => scaled(held ? point.positionsValue : point.value)),
          borderColor: palette.primary,
          backgroundColor: palette.primarySoft,
          fill: false,
          tension: 0.2,
          borderWidth: 2,
          pointRadius: points.map((point) => (reportDates.has(point.date) ? 5 : 0)),
          pointBackgroundColor: palette.tax,
          pointHoverRadius: 6,
        },
        {
          type: 'line' as const,
          label: held
            ? translate('overview.chart.paidForAssets')
            : translate('overview.chart.contributed'),
          data: points.map((point) => scaled(held ? point.costBasis : point.netContributions)),
          borderColor: palette.neutral,
          borderDash: [6, 4],
          stepped: true,
          pointRadius: 0,
          borderWidth: 1.5,
          fill: false,
        },
        ...(this.hasPlan() && this.showPlan() && !held
          ? [
              {
                type: 'line' as const,
                label: translate('overview.chart.valueToPlan'),
                data: points.map((point) => scaled(point.planValue)),
                borderColor: plan,
                pointRadius: 0,
                borderWidth: 2,
                tension: 0.2,
                fill: false,
              },
              {
                type: 'line' as const,
                label: translate('overview.chart.contributedToPlan'),
                data: points.map((point) => scaled(point.planNetContributions)),
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

  protected readonly hiddenLines = new HiddenLines();

  protected readonly data = computed(() => {
    const lines = this.allLines();

    return { ...lines, datasets: this.hiddenLines.applyTo(lines.datasets) };
  });

  protected readonly legend = computed(() => this.hiddenLines.legendOf(this.allLines().datasets));

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const { currency } = this.scale();
    const held = this.showHeld();
    const reportDays = new Map(
      [...this.reportsByBucketDate()].map(([date, report]) => [formatDate(date), report]),
    );
    const phone = this.viewport.phone();

    return {
      ...lineChartBase(),
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: (item: TooltipItem) =>
              `${item.dataset.label}: ${formatMoney(item.parsed.y, currency, 0)}`,
            afterBody: (items: TooltipItem[]) => {
              const report = reportDays.get(items[0]?.label);
              const gain = held && items.length >= 2 ? items[0].parsed.y - items[1].parsed.y : null;

              return [
                ...(gain === null
                  ? []
                  : [
                      translate('overview.chart.gainOnAssetsTooltip', {
                        gain: `${gain >= 0 ? '+' : ''}${formatMoney(gain, currency, 0)}`,
                      }),
                    ]),
                ...(report
                  ? [
                      translate('overview.chart.report', {
                        report:
                          report.kind === 'RiskReview'
                            ? translate('overview.chart.riskReview')
                            : formatPeriod(report.period),
                      }),
                    ]
                  : []),
              ];
            },
          },
        },
      },
      scales: {
        x: dateAxis(palette, {
          phone,
          maxTicks: phone ? phoneMaxTicks : this.buckets().length > 60 ? 8 : 12,
          shortLabels: phone,
        }),
        y: {
          ...valueAxis(palette, {
            phone,
            maxTicks: phone ? 5 : undefined,
            tick: (value) => (phone ? formatAxisNumber(value) : formatMoney(value, currency, 0)),
          }),
        },
      },
    };
  });
}

interface TooltipItem {
  label: string;
  parsed: { y: number };
  dataset: { label: string };
}

function readChoice(key: string, fallback: boolean): boolean {
  try {
    const stored = localStorage.getItem(key);

    return stored === null ? fallback : stored !== 'false';
  } catch {
    return fallback;
  }
}

function remember(key: string, choice: boolean): void {
  try {
    localStorage.setItem(key, String(choice));
  } catch {}
}
