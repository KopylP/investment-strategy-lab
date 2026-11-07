import type { TooltipModel } from 'chart.js';
import { ChartReadoutLine } from './chart-readout-line';

export interface ChartReadout {
  title: string;
  lines: ChartReadoutLine[];
}

const labelSeparator = ': ';

export function readoutOf(tooltip: TooltipModel<never>): ChartReadout | null {
  if (tooltip.opacity === 0 || !tooltip.body?.length) return null;

  const lines = tooltip.body.flatMap((body, index) =>
    body.lines.map((text) => readoutLine(text, colorOf(tooltip, index))),
  );

  return { title: tooltip.title.join(' '), lines };
}

function readoutLine(text: string, color: string): ChartReadoutLine {
  const separator = text.lastIndexOf(labelSeparator);
  if (separator < 0) return { color, label: '', value: text };

  return {
    color,
    label: text.slice(0, separator),
    value: text.slice(separator + labelSeparator.length),
  };
}

function colorOf(tooltip: TooltipModel<never>, index: number): string {
  const colors = tooltip.labelColors[index];
  const border = colors?.borderColor;

  return typeof border === 'string' ? border : 'currentColor';
}
