import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { ProgressSpinner } from 'primeng/progressspinner';
import { SelectButton } from 'primeng/selectbutton';
import { ChartTheme } from '../../../../shared/charts/chart-theme';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { formatAxisNumber, formatDate, formatPercent } from '../../../../shared/pipes/format';
import { DatePipe, MoneyPipe, PercentPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { Viewport } from '../../../../shared/ui/viewport';
import { BacktestStore } from '../../data-access/backtest-store';
import { ReplaySeriesDto, ReplaySeriesKind } from '../../models/backtest-dto';

const FEW_PERIODS = 24;

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
  readonly currency = input.required<string>();
  readonly benchmarkLabel = input.required<string>();

  protected readonly store = inject(BacktestStore);
  protected readonly viewport = inject(Viewport);
  private readonly theme = inject(ChartTheme);

  protected readonly fewPeriods = FEW_PERIODS;
  protected readonly periodOptions = [
    { label: '5 років', value: 5 },
    { label: '10 років', value: 10 },
    { label: '15 років', value: 15 },
  ];

  protected readonly result = computed(() => this.store.backtest()?.result ?? null);
  protected readonly strategySummary = computed(
    () => this.result()?.series.find((series) => series.kind === 'Strategy')?.summary ?? null,
  );
  protected readonly rows = computed(() =>
    (this.result()?.series ?? []).map((series) => ({
      label: this.labelOf(series.kind),
      summary: series.summary,
    })),
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

  private labelOf(kind: ReplaySeriesKind): string {
    if (kind === 'Strategy') return 'Стратегія';

    return kind === 'Benchmark' ? this.benchmarkLabel() : 'Порівну в усі фонди';
  }

  private colorOf(kind: ReplaySeriesKind): string {
    const palette = this.theme.palette();

    if (kind === 'Strategy') return palette.primary;

    return kind === 'Benchmark' ? palette.series[1] : palette.neutral;
  }

  private dataOf(valueOf: (point: ReplaySeriesDto['points'][number]) => number) {
    const series = this.result()?.series ?? [];

    return {
      labels: (series[0]?.points ?? []).map((point) => formatDate(point.date)),
      datasets: series.map((each) => ({
        label: this.labelOf(each.kind),
        data: each.points.map(valueOf),
        borderColor: this.colorOf(each.kind),
        borderWidth: each.kind === 'Strategy' ? 2 : 1.5,
        pointRadius: 0,
        tension: 0.1,
        fill: false,
      })),
    };
  }

  private optionsOf(tick: (value: number) => string) {
    const palette = this.theme.palette();

    return {
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: 'index', intersect: false },
      plugins: { legend: { labels: { color: palette.text, usePointStyle: true } } },
      scales: {
        x: { ticks: { color: palette.muted, maxTicksLimit: 8 }, grid: { display: false } },
        y: {
          ticks: { color: palette.muted, callback: (value: number) => tick(value) },
          grid: { color: palette.grid },
        },
      },
    };
  }
}
