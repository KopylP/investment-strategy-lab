import type { Chart, Plugin } from 'chart.js';

const lineColor = 'rgba(100, 116, 139, 0.5)';

export const scrubLine: Plugin = {
  id: 'scrubLine',
  afterDatasetsDraw(chart: Chart) {
    const active = chart.tooltip?.getActiveElements() ?? [];
    if (!active.length || !chart.scales['x']) return;

    drawVerticalLine(chart, active[0].element.x);
  },
};

function drawVerticalLine(chart: Chart, x: number): void {
  const { ctx, chartArea } = chart;

  ctx.save();
  ctx.strokeStyle = lineColor;
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(x, chartArea.top);
  ctx.lineTo(x, chartArea.bottom);
  ctx.stroke();
  ctx.restore();
}
