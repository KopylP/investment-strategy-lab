import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  resource,
} from '@angular/core';
import { Message } from 'primeng/message';
import { ApiClient } from '../../../../core/api/api-client';
import { BacktestDto, STRATEGY_SERIES } from '../../../backtesting/models/backtest-dto';
import { ChartTheme } from '../../../../shared/charts/chart-theme';
import { ChartLegend } from '../../../../shared/charts/chart-legend/chart-legend';
import { HiddenLines } from '../../../../shared/charts/hidden-lines';
import { LiveChart } from '../../../../shared/charts/live-chart';
import { lineChartBase } from '../../../../shared/charts/line-chart-base';
import { dateAxis } from '../../../../shared/charts/date-axis';
import { valueAxis } from '../../../../shared/charts/value-axis';
import { formatAxisNumber, formatDate, formatPercent } from '../../../../shared/pipes/format';
import { Viewport } from '../../../../shared/ui/viewport';
import { TrialRunDto } from '../../models/trial-run-dto';

import { TrialMetric } from '../../models/trial-metric';
import { ExperimentsApi } from '../../data-access/experiments-api';
import { experimentBacktest } from '../../data-access/experiment-result-mapper';
import { BacktestComparisonDto, CompareBacktestsBody } from '../../models/backtest-comparison-dto';
import { activeLocale } from '../../../../core/i18n/language';
const metrics: { key: TrialMetric; label: string }[] = [
  { key: 'annualReturn', label: translate('builder.comparison.yearlyReturn') },
  { key: 'annualReturnAfterTax', label: translate('backtest.afterTax') },
  { key: 'volatility', label: translate('backtest.volatility') },
  { key: 'sharpe', label: 'Sharpe' },
  { key: 'maxDrawdown', label: translate('backtest.largestFall') },
];

@Component({
  selector: 'app-trial-comparison',
  imports: [TranslocoPipe, Message, ChartLegend, LiveChart],
  templateUrl: './trial-comparison.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrialComparison {
  readonly first = input.required<TrialRunDto>();
  readonly second = input.required<TrialRunDto>();
  private readonly api = inject(ApiClient);
  private readonly experiments = inject(ExperimentsApi);
  private readonly theme = inject(ChartTheme);
  private readonly viewport = inject(Viewport);
  protected readonly hiddenLines = new HiddenLines();
  private readonly results = resource({
    params: () => [this.first(), this.second()],
    loader: ({ params }) =>
      Promise.all(
        params.map((run) => this.load(run)),
      ),
  });
  protected readonly error = this.results.error;
  private readonly comparison = resource({
    params: (): CompareBacktestsBody => ({
      first: this.reference(this.first()),
      second: this.reference(this.second()),
    }),
    loader: ({ params }) => this.api.post<BacktestComparisonDto>('/api/backtesting/comparisons', params),
  });
  protected readonly comparisonReason = computed(() => this.comparison.hasValue() ? this.comparison.value().reason : null);
  protected readonly comparisonError = this.comparison.error;

  private reference(run: TrialRunDto) {
    return { strategyId: run.strategyId, version: run.version, experimentId: run.experimentId ?? null };
  }

  private async load(run: TrialRunDto): Promise<BacktestDto> {
    if (run.experimentId) {
      const experiment = await this.experiments.get(run.experimentId);
      return experimentBacktest(experiment);
    }

    return this.api.get<BacktestDto>(`/api/backtesting/strategies/${run.strategyId}`, { version: run.version });
  }
  protected readonly rows = computed(() =>
    metrics.map(({ key, label }) => {
      const first = this.first()[key];
      const second = this.second()[key];
      const difference = this.comparison.hasValue() ? this.comparison.value().difference?.[key] ?? null : null;

      return {
        label,
        first: this.format(first, key),
        second: this.format(second, key),
        difference: this.format(difference, key),
      };
    }),
  );
  private readonly lines = computed(() => {
    const results = this.results.hasValue() ? this.results.value() : [];
    const first = results[0]?.result?.series ?? [];
    const second = results[1]?.result?.series.find((series) => series.code === STRATEGY_SERIES);
    const lines = first.map((series) => ({
      ...series,
      label:
        series.code === STRATEGY_SERIES
          ? translate('builder.comparison.runA', { number: this.first().number })
          : series.label,
    }));
    if (second) lines.push({ ...second, label: translate('builder.comparison.runB', { number: this.second().number }) });

    return lines;
  });
  private readonly dates = computed(() =>
    [...new Set(this.lines().flatMap((line) => line.points.map((point) => point.date)))].sort(),
  );
  private readonly datasets = computed(() =>
    this.lines().map((line, index) => {
      const values = new Map(line.points.map((point) => [point.date, point.growth * 100]));
      const colors = this.theme.palette().series;

      return {
        label: line.label,
        data: this.dates().map((date) => values.get(date) ?? null),
        borderColor: colors[index % colors.length],
        borderWidth: line.code === STRATEGY_SERIES ? 2 : 1.5,
        pointRadius: 0,
        tension: 0.1,
        fill: false,
      };
    }),
  );
  protected readonly legend = computed(() => this.hiddenLines.legendOf(this.datasets()));
  protected readonly data = computed(() => ({
    labels: this.dates().map(formatDate),
    datasets: this.hiddenLines.applyTo(this.datasets()),
  }));
  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const phone = this.viewport.phone();

    return {
      ...lineChartBase(),
      plugins: { legend: { display: false } },
      scales: {
        x: dateAxis(palette, { phone, maxTicks: phone ? 4 : 8, shortLabels: phone }),
        y: valueAxis(palette, { phone, tick: (value: number) => formatAxisNumber(value) }),
      },
    };
  });

  private format(value: number | null, metric: TrialMetric): string {
    if (value === null) return '—';

    return metric === 'sharpe'
      ? value.toLocaleString(activeLocale, { maximumFractionDigits: 2 })
      : formatPercent(value);
  }
}
