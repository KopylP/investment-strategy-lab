import { ChartPalette } from './chart-theme';

const PHONE_FONT_SIZE = 11;
const PHONE_MARKER_SIZE = 6;
const PHONE_PADDING = 10;

export function chartLegendLabels(palette: ChartPalette, phone: boolean) {
  const labels = { color: palette.text, usePointStyle: true };

  if (!phone) return labels;

  return {
    ...labels,
    boxWidth: PHONE_MARKER_SIZE,
    boxHeight: PHONE_MARKER_SIZE,
    padding: PHONE_PADDING,
    font: { size: PHONE_FONT_SIZE },
  };
}
