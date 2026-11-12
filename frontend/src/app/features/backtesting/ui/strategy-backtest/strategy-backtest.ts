import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { ProgressSpinner } from 'primeng/progressspinner';
import { SelectButton } from 'primeng/selectbutton';
import { ChartTheme } from '../../../../shared/charts/chart-theme';
import { ChartLegend } from '../../../../shared/charts/chart-legend/chart-legend';
import { dateAxis } from '../../../../shared/charts/date-axis';
import { HiddenLines } from '../../../../shared/charts/hidden-lines';
import { lineChartBase } from '../../../../shared/charts/line-chart-base';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { valueAxis } from '../../../../shared/charts/value-axis';
import { formatAxisNumber, formatDate, formatPercent } from '../../../../shared/pipes/format';
import {
  DatePipe,
  MoneyPipe,
  PercentPipe,
  SignedPercentPipe,
} from '../../../../shared/pipes/format-pipes';
import { Viewport } from '../../../../shared/ui/viewport';
import { BacktestStore } from '../../data-access/backtest-store';
import {
  ReplayPointDto,
  ReplaySeriesDto,
  ReplayResultDto,
  STRATEGY_SERIES,
} from '../../models/backtest-dto';

const FEW_PERIODS = 24;
const PHONE_DATE_TICKS = 4;
const DESKTOP_DATE_TICKS = 8;
const PHONE_VALUE_TICKS = 4;
const STRATEGY_LINE_WIDTH = 2;
const COMPARISON_LINE_WIDTH = 1.5;

@Component({
  selector: 'app-strategy-backtest',
  host: { class: 'contents' },
  imports: [
    TranslocoPipe,
    FormsModule,
    InputNumber,
    Message,
    ProgressSpinner,
    SelectButton,
    ChartLegend,
    LiveChart,
    DatePipe,
    MoneyPipe,
    PercentPipe,
    SignedPercentPipe,
  ],
  providers: [BacktestStore],
  templateUrl: './strategy-backtest.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyBacktest {
  readonly strategyId = input.required<string>();
  readonly version = input.required<number>();
  readonly replay = input<ReplayResultDto | null>(null);

  protected readonly store = inject(BacktestStore);
  protected readonly viewport = inject(Viewport);
  private readonly theme = inject(ChartTheme);

  protected readonly fewPeriods = FEW_PERIODS;
  protected readonly periodOptions = [5, 10, 15, 20].map((years) => ({
    label: translate('backtest.years', { count: years }),
    value: years,
  }));

  protected readonly result = computed(
    () => this.replay() ?? this.store.backtest()?.result ?? null,
  );
  protected readonly currency = computed(() => this.result()?.currency ?? '');
  protected readonly series = computed(() => this.result()?.series ?? []);
  protected readonly strategySummary = computed(
    () => this.series().find((series) => series.code === STRATEGY_SERIES)?.summary ?? null,
  );

  protected readonly outcomes = computed(() => this.store.outcomes());
  protected readonly strategyOutcomes = computed(
    () =>
      this.outcomes()?.series.find((series) => series.code === STRATEGY_SERIES)?.outcomes ?? null,
  );
  protected readonly hiddenLines = new HiddenLines();

  protected readonly lines = computed(() =>
    this.series().map((series, index) => ({ ...series, color: this.colorAt(index) })),
  );
  protected readonly comparedOutcomes = computed(() => {
    const colors = new Map(this.lines().map((line) => [line.code, line.color]));
    const withHistory = (this.outcomes()?.series ?? []).filter(
      (series) => series.outcomes.periods > 0,
    );

    return withHistory.map((series) => ({ ...series, color: colors.get(series.code) ?? '' }));
  });

  protected readonly legend = computed(() =>
    this.hiddenLines.legendOf(this.datasetsOf((point) => point.growth)),
  );
  protected readonly growthData = computed(() => this.dataOf((point) => point.growth * 100));
  protected readonly drawdownData = computed(() =>
    this.dataOf((point) => point.worstDrawdown * 100),
  );
  protected readonly growthOptions = computed(() =>
    this.optionsOf((value) => formatAxisNumber(value)),
  );
  protected readonly drawdownOptions = computed(() =>
    this.optionsOf((value) => formatPercent(value / 100, 0)),
  );

  constructor() {
    effect(() => {
      if (this.replay()) this.store.clear();
      else this.store.show(this.strategyId(), this.version());
    });
  }

  private dataOf(valueOf: (point: ReplayPointDto) => number) {
    const series = this.series();
    const dates = (series[0]?.points ?? []).map((point) => formatDate(point.date));

    return { labels: dates, datasets: this.hiddenLines.applyTo(this.datasetsOf(valueOf)) };
  }

  private datasetsOf(valueOf: (point: ReplayPointDto) => number) {
    return this.series().map((series, index) => this.datasetOf(series, index, valueOf));
  }

  private datasetOf(
    series: ReplaySeriesDto,
    index: number,
    valueOf: (point: ReplayPointDto) => number,
  ) {
    const isStrategy = series.code === STRATEGY_SERIES;

    return {
      label: series.label,
      data: series.points.map(valueOf),
      borderColor: this.colorAt(index),
      borderWidth: isStrategy ? STRATEGY_LINE_WIDTH : COMPARISON_LINE_WIDTH,
      pointRadius: 0,
      tension: 0.1,
      fill: false,
    };
  }

  private colorAt(index: number): string {
    const colors = this.theme.palette().series;

    return colors[index % colors.length];
  }

  private optionsOf(tick: (value: number) => string) {
    const palette = this.theme.palette();
    const phone = this.viewport.phone();
    const maxDateTicks = phone ? PHONE_DATE_TICKS : DESKTOP_DATE_TICKS;
    const maxValueTicks = phone ? PHONE_VALUE_TICKS : undefined;

    return {
      ...lineChartBase(),
      plugins: { legend: { display: false } },
      scales: {
        x: dateAxis(palette, { phone, maxTicks: maxDateTicks, shortLabels: phone }),
        y: valueAxis(palette, { phone, tick, maxTicks: maxValueTicks }),
      },
    };
  }
}
