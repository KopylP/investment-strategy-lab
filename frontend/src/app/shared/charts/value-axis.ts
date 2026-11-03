import { ChartPalette } from './chart-theme';

const PHONE_FONT_SIZE = 11;

export interface ValueAxis {
  phone: boolean;
  tick: (value: number) => string;
  maxTicks?: number;
}

export function valueAxis(palette: ChartPalette, axis: ValueAxis) {
  const ticks = {
    color: palette.muted,
    maxTicksLimit: axis.maxTicks,
    font: axis.phone ? { size: PHONE_FONT_SIZE } : undefined,
    callback: axis.tick,
  };

  return { ticks, grid: { color: palette.grid } };
}
