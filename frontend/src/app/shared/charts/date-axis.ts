import { shortenDate } from '../pipes/format';
import { ChartPalette } from './chart-theme';

const PHONE_FONT_SIZE = 11;

export interface DateAxis {
  phone: boolean;
  maxTicks: number;
  shortLabels: boolean;
}

export function dateAxis(palette: ChartPalette, axis: DateAxis) {
  const ticks = {
    color: palette.muted,
    maxTicksLimit: axis.maxTicks,
    maxRotation: 0,
    font: axis.phone ? { size: PHONE_FONT_SIZE } : undefined,
    ...(axis.shortLabels ? { callback: shortDateLabel } : {}),
  };

  return { ticks, grid: { display: false } };
}

function shortDateLabel(
  this: { getLabelForValue: (value: number) => string },
  value: number,
): string {
  return shortenDate(this.getLabelForValue(value));
}
