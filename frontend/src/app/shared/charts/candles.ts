import { translate } from '@jsverse/transloco';
import { ChartPalette } from './chart-theme';
import { PricePoint } from './price-point';
import { SeriesBucket } from './series-buckets';

export interface Candle {
  open: number;
  high: number;
  low: number;
  close: number;
}

const WICK_WIDTH = 1.5;
const MAX_BODY_WIDTH = 14;
const MIN_BODY_HEIGHT = 2;
const BODY_SHARE_OF_SLOT = 0.7;

export function candlesOf(buckets: SeriesBucket<PricePoint>[]): Candle[] {
  const candles: Candle[] = [];

  for (const bucket of buckets) {
    const firstDay = bucket.points[0];
    const open = firstDay.open ?? candles.at(-1)?.close ?? firstDay.close;
    const close = bucket.last.close;
    const highs = bucket.points.map((point) => point.high ?? point.close);
    const lows = bucket.points.map((point) => point.low ?? point.close);

    candles.push({
      open,
      close,
      high: Math.max(open, close, ...highs),
      low: Math.min(open, close, ...lows),
    });
  }

  return candles;
}

export function candleDatasets(candles: Candle[], palette: ChartPalette, label: string) {
  const colors = candles.map((candle) =>
    candle.close >= candle.open ? palette.gain : palette.loss,
  );

  return [
    {
      type: 'bar' as const,
      label,
      data: candles.map((candle) => [candle.low, candle.high]),
      backgroundColor: colors,
      borderColor: palette.primary,
      borderWidth: 0,
      grouped: false,
      barThickness: WICK_WIDTH,
    },
    {
      type: 'bar' as const,
      label,
      data: candles.map((candle) => [
        Math.min(candle.open, candle.close),
        Math.max(candle.open, candle.close),
      ]),
      backgroundColor: colors,
      borderColor: palette.primary,
      borderWidth: 0,
      grouped: false,
      barPercentage: BODY_SHARE_OF_SLOT,
      categoryPercentage: 1,
      maxBarThickness: MAX_BODY_WIDTH,
      minBarLength: MIN_BODY_HEIGHT,
      isCandleBody: true,
    },
  ];
}

export function candleTooltipLines(
  candle: Candle,
  formatValue: (value: number) => string,
): string[] {
  return [
    `${translate('chart.candle.open')}: ${formatValue(candle.open)}`,
    `${translate('chart.candle.high')}: ${formatValue(candle.high)}`,
    `${translate('chart.candle.low')}: ${formatValue(candle.low)}`,
    `${translate('chart.candle.close')}: ${formatValue(candle.close)}`,
  ];
}
