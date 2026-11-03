import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { ChartLegend } from './chart-legend/chart-legend';
import { dateAxis } from './date-axis';
import { HiddenLines } from './hidden-lines';
import { LiveChart } from './live-chart';
import { valueAxis } from './value-axis';
import { formatAxisNumber, formatDate, formatMoney, formatQuantity } from '../pipes/format';
import { Viewport } from '../ui/viewport';
import { ChartTheme, withAlpha } from './chart-theme';

export interface PricePoint {
  date: string;
  close: number;
}

export interface TradeMarker {
  date: string;
  side: 'Buy' | 'Sell';
  quantity: number;
  price: number;
  currency: string;
}

@Component({
  selector: 'app-price-chart',
  imports: [ChartLegend, LiveChart],
  template: `@if (legend().length) {
      <app-chart-legend
        class="block mb-2"
        [lines]="legend()"
        (toggled)="hiddenLines.toggle($event)"
      />
    }
    <app-live-chart
      [data]="data()"
      [options]="options()"
      [plugins]="[tradeLines()]"
      [height]="viewport.phone() ? phoneHeight() : height()"
    />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PriceChart {
  private readonly theme = inject(ChartTheme);
  protected readonly viewport = inject(Viewport);

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

  private readonly tradesByIndex = computed(() => {
    const points = this.points();
    const byIndex = new Map<number, TradeMarker[]>();

    for (const trade of this.trades()) {
      const index = points.findIndex((point) => point.date >= trade.date);
      if (index < 0) continue;

      byIndex.set(index, [...(byIndex.get(index) ?? []), trade]);
    }

    return byIndex;
  });

  private readonly allLines = computed(() => {
    const points = this.points();
    const palette = this.theme.palette();
    const byIndex = this.tradesByIndex();
    const markers = (side: TradeMarker['side']) =>
      points.map((point, index) =>
        byIndex.get(index)?.some((trade) => trade.side === side) ? point.close : null,
      );

    return {
      labels: points.map((point) => formatDate(point.date)),
      datasets: [
        {
          label: 'Ціна закриття',
          data: points.map((point) => point.close),
          borderColor: this.color(),
          backgroundColor: withAlpha(this.color(), 0.12),
          fill: true,
          tension: 0.15,
          borderWidth: 2,
          pointRadius: 0,
          pointHoverRadius: 4,
          order: 2,
        },
        ...(byIndex.size
          ? [
              {
                label: 'Купівля',
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
                label: 'Продаж',
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
    this.hiddenLines.legendOf(
      this.allLines().datasets.filter((dataset) => dataset.showLine === false),
    ),
  );

  protected readonly options = computed(() => {
    const palette = this.theme.palette();
    const currency = this.currency();
    const byIndex = this.tradesByIndex();
    const closes = this.points().map((point) => point.close);
    const digits = Math.max(...closes) - Math.min(...closes) < 50 ? 2 : 0;
    const phone = this.viewport.phone();

    return {
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { display: false },
        tooltip: {
          filter: (item: { raw: unknown }) => item.raw !== null,
          callbacks: {
            label: (context: {
              datasetIndex: number;
              dataIndex: number;
              parsed: { y: number };
            }) => {
              if (context.datasetIndex === 0) return formatMoney(context.parsed.y, currency);

              const side = context.datasetIndex === 1 ? 'Buy' : 'Sell';

              return (byIndex.get(context.dataIndex) ?? [])
                .filter((trade) => trade.side === side)
                .map(
                  (trade) =>
                    `${side === 'Buy' ? 'Купівля' : 'Продаж'} ${formatQuantity(trade.quantity)} шт. × ` +
                    `${formatMoney(trade.price, trade.currency)} (${formatDate(trade.date)})`,
                );
            },
          },
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
        },
      },
    };
  });
}
