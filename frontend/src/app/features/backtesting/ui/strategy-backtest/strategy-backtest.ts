import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { ProgressSpinner } from 'primeng/progressspinner';
import { SelectButton } from 'primeng/selectbutton';
import { chartLegendLabels } from '../../../../shared/charts/chart-legend';
import { ChartTheme } from '../../../../shared/charts/chart-theme';
import { dateAxis } from '../../../../shared/charts/date-axis';
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
import { ReplayPointDto, ReplaySeriesDto, STRATEGY_SERIES } from '../../models/backtest-dto';

const FEW_PERIODS = 24;
const PHONE_DATE_TICKS = 4;
const DESKTOP_DATE_TICKS = 8;
const PHONE_VALUE_TICKS = 4;
const STRATEGY_LINE_WIDTH = 2;
const COMPARISON_LINE_WIDTH = 1.5;

@Component({
  selector: 'app-strategy-backtest',
  imports: [
    FormsModule,
    InputNumber,
    Message,
    ProgressSpinner,
    SelectButton,
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

  protected readonly store = inject(BacktestStore);
  protected readonly viewport = inject(Viewport);
  private readonly theme = inject(ChartTheme);

  protected readonly fewPeriods = FEW_PERIODS;
  protected readonly periodOptions = [
    { label: '5 років', value: 5 },
    { label: '10 років', value: 10 },
    { label: '15 років', value: 15 },
    { label: '20 років', value: 20 },
  ];

  protected readonly result = computed(() => this.store.backtest()?.result ?? null);
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
  protected readonly comparedOutcomes = computed(() =>
    (this.outcomes()?.series ?? []).filter((series) => series.outcomes.periods > 0),
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
    effect(() => this.store.show(this.strategyId(), this.version()));
  }

  private dataOf(valueOf: (point: ReplayPointDto) => number) {
    const series = this.series();
    const dates = (series[0]?.points ?? []).map((point) => formatDate(point.date));

    return {
      labels: dates,
      datasets: series.map((each, index) => this.datasetOf(each, index, valueOf)),
    };
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
      plugins: { legend: { labels: chartLegendLabels(palette, phone) } },
      scales: {
        x: dateAxis(palette, { phone, maxTicks: maxDateTicks, shortLabels: phone }),
        y: valueAxis(palette, { phone, tick, maxTicks: maxValueTicks }),
      },
    };
  }
}
