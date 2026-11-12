import { translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ChartStyle } from '../../core/context/chart-style';
import { candleDatasets, candlesOf, candleTooltipLines } from './candles';
import { ChartGranularity } from './chart-granularity';
import { ChartLegend } from './chart-legend/chart-legend';
import { dateAxis } from './date-axis';
import { GranularityToggle } from './granularity-toggle';
import { HiddenLines } from './hidden-lines';
import { LiveChart } from './live-chart';
import { PricePoint } from './price-point';
import { bucketsOf } from './series-buckets';
import { valueAxis } from './value-axis';
import { formatAxisNumber, formatDate, formatMoney, formatQuantity } from '../pipes/format';
import { Viewport } from '../ui/viewport';
import { ChartTheme, withAlpha } from './chart-theme';

export interface TradeMarker {
  date: string;
  side: 'Buy' | 'Sell';
  quantity: number;
  price: number;
  currency: string;
}

@Component({
  selector: 'app-price-chart',
  imports: [ChartLegend, GranularityToggle, LiveChart],
  template: `@if (candles()) {
      <div class="flex justify-end mb-2">
        <app-granularity-toggle />
      </div>
    }
    @if (legend().length) {
      <app-chart-legend
        class="block mb-2"
        [lines]="legend()"
        (toggled)="hiddenLines.toggle($event)"
      />
    }
    <app-live-chart
      [type]="candles() ? 'bar' : 'line'"
      [data]="data()"
      [options]="options()"
      [plugins]="[tradeLines()]"
      [height]="viewport.phone() ? phoneHeight() : height()"
    />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PriceChart {
  private readonly theme = inject(ChartTheme);
  private readonly chartStyle = inject(ChartStyle);
  private readonly granularity = inject(ChartGranularity);
  protected readonly viewport = inject(Viewport);
  protected readonly candles = this.chartStyle.isCandles;

  readonly points = input.required<PricePoint[]>();
  readonly currency = input.required<string>();
  readonly trades = input<TradeMarker[]>([]);
  readonly height = input('18rem');
  readonly phoneHeight = input('14rem');

  private readonly color = computed(() => {
    const points = this.points();
    const palette = this.theme.palette();

    return (points.at(-1)?.close ?? 0) >= (points.at(0)?.close ?? 0) ? palette.gain : palette.loss;
  });

  private readonly buckets = computed(() =>
    bucketsOf(this.points(), this.candles() ? this.granularity.step() : 'day'),
  );

  private readonly candleSeries = computed(() =>
    this.candles() ? candlesOf(this.buckets()) : null,
  );

  private readonly tradesByIndex = computed(() => {
    const buckets = this.buckets();
    const byIndex = new Map<number, TradeMarker[]>();

    for (const trade of this.trades()) {
      const index = buckets.findIndex((bucket) => bucket.date >= trade.date);
      if (index < 0) continue;

      byIndex.set(index, [...(byIndex.get(index) ?? []), trade]);
    }

    return byIndex;
  });

  private readonly priceDatasets = computed(() => {
    const buckets = this.buckets();
    const palette = this.theme.palette();
    const candles = this.candleSeries();
    const label = translate('chart.closingPrice');

    if (candles) return candleDatasets(candles, palette, label);

    return [
      {
        type: 'line' as const,
        label,
        data: buckets.map((bucket) => bucket.last.close),
        borderColor: this.color(),
        backgroundColor: withAlpha(this.color(), 0.12),
        fill: true,
        tension: 0.15,
        borderWidth: 2,
        pointRadius: 0,
        pointHoverRadius: 4,
        order: 2,
      },
    ];
  });

  private readonly allLines = computed(() => {
    const buckets = this.buckets();
    const palette = this.theme.palette();
    const byIndex = this.tradesByIndex();
    const markers = (side: TradeMarker['side']) =>
      buckets.map((bucket, index) =>
        byIndex.get(index)?.some((trade) => trade.side === side) ? bucket.last.close : null,
      );

    return {
      labels: buckets.map((bucket) => formatDate(bucket.date)),
      datasets: [
        ...this.priceDatasets(),
        ...(byIndex.size
          ? [
              {
                type: 'line' as const,
                side: 'Buy' as TradeMarker['side'],
                label: translate('chart.buy'),
                data: markers('Buy'),
                showLine: false,
                pointStyle: 'triangle',
                pointRadius: 9,
                pointHoverRadius: 11,
                backgroundColor: palette.primary,
                borderColor: '#ffffff',
                borderWidth: 2,
                order: 0,
              },
              {
                type: 'line' as const,
                side: 'Sell' as TradeMarker['side'],
                label: translate('chart.sell'),
                data: markers('Sell'),
                showLine: false,
                pointStyle: 'triangle',
                rotation: 180,
                pointRadius: 9,
                pointHoverRadius: 11,
                backgroundColor: palette.tax,
                borderColor: '#ffffff',
                borderWidth: 2,
                order: 0,
              },
            ]
          : []),
      ],
    };
  });

  protected readonly tradeLines = computed(() => {
    const byIndex = this.tradesByIndex();
    const palette = this.theme.palette();

    return {
      id: 'tradeLines',
      beforeDatasetsDraw: (chart: {
        ctx: CanvasRenderingContext2D;
        chartArea: { top: number; bottom: number };
        scales: { x: { getPixelForValue: (index: number) => number } };
      }) => {
        const { ctx, chartArea, scales } = chart;

        ctx.save();
        ctx.setLineDash([4, 4]);
        ctx.lineWidth = 1;

        for (const [index, trades] of byIndex) {
          const x = scales.x.getPixelForValue(index);

          ctx.strokeStyle = withAlpha(
            trades.some((trade) => trade.side === 'Buy') ? palette.primary : palette.tax,
            0.55,
          );
          ctx.beginPath();
          ctx.moveTo(x, chartArea.top);
          ctx.lineTo(x, chartArea.bottom);
          ctx.stroke();
        }

        ctx.restore();
      },
    };
  });

  protected readonly hiddenLines = new HiddenLines();

  protected readonly data = computed(() => {
    const lines = this.allLines();

    return { ...lines, datasets: this.hiddenLines.applyTo(lines.datasets) };
  });

  protected readonly legend = computed(() =>
    this.hiddenLines.legendOf(this.allLines().datasets.filter((dataset) => 'side' in dataset)),
  );

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const currency = this.currency();
    const closes = this.points().map((point) => point.close);
    const digits = Math.max(...closes) - Math.min(...closes) < 50 ? 2 : 0;
    const phone = this.viewport.phone();

    return {
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: {
          filter: (item: TooltipItem) => item.raw !== null && !item.dataset.isCandleBody,
          callbacks: { label: (item: TooltipItem) => this.tooltipLinesOf(item) },
        },
      },
      scales: {
        x: dateAxis(palette, { phone, maxTicks: phone ? 3 : 6, shortLabels: phone }),
        y: {
          ...valueAxis(palette, {
            phone,
            maxTicks: phone ? 5 : undefined,
            tick: (value) =>
              phone ? formatAxisNumber(value, digits) : formatMoney(value, currency, digits),
          }),
          position: 'right',
          beginAtZero: this.candles() ? false : undefined,
        },
      },
    };
  });

  private tooltipLinesOf(item: TooltipItem): string | string[] {
    const currency = this.currency();
    const candle = this.candleSeries()?.[item.dataIndex];

    if (item.dataset.side) return this.tradeLinesOf(item.dataset.side, item.dataIndex);
    if (candle) return candleTooltipLines(candle, (value) => formatMoney(value, currency));

    return formatMoney(item.parsed.y, currency);
  }

  private tradeLinesOf(side: TradeMarker['side'], index: number): string[] {
    const trades = this.tradesByIndex().get(index) ?? [];
    const sideTrades = trades.filter((trade) => trade.side === side);

    return sideTrades.map((trade) =>
      translate('chart.tradeTooltip', {
        side: translate(side === 'Buy' ? 'chart.buy' : 'chart.sell'),
        quantity: formatQuantity(trade.quantity),
        price: formatMoney(trade.price, trade.currency),
        date: formatDate(trade.date),
      }),
    );
  }
}

interface TooltipItem {
  raw: unknown;
  dataIndex: number;
  parsed: { y: number };
  dataset: { side?: TradeMarker['side']; isCandleBody?: boolean };
}
