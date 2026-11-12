import { ChartPalette } from './chart-theme';

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

export function candlesOf(valuesPerBucket: number[][]): Candle[] {
  const candles: Candle[] = [];

  for (const values of valuesPerBucket) {
    const open = candles.at(-1)?.close ?? values[0];
    const close = values[values.length - 1];

    candles.push({
      open,
      close,
      high: Math.max(open, ...values),
      low: Math.min(open, ...values),
    });
  }

  return candles;
}

export function candleDatasets(candles: Candle[], palette: ChartPalette, label: string) {
  const colors = candles.map((candle) => (candle.close >= candle.open ? palette.gain : palette.loss));

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
