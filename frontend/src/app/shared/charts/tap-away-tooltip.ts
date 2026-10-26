import type { Chart, Plugin } from 'chart.js';

const listeners = new WeakMap<Chart, (event: PointerEvent) => void>();

/**
 * Closes a chart's tooltip when the pointer goes down anywhere but the chart. A touch never sends the "mouseout" that
 * hides it on a desktop, so on a phone the tooltip would stay over the lines until the chart is tapped again.
 */
export const tapAwayTooltip: Plugin = {
  id: 'tapAwayTooltip',
  afterInit(chart) {
    const listener = (event: PointerEvent) => {
      if (event.target === chart.canvas || !chart.tooltip?.getActiveElements().length) return;
      chart.setActiveElements([]);
      chart.tooltip.setActiveElements([], { x: 0, y: 0 });
      chart.update('none');
    };
    document.addEventListener('pointerdown', listener, true);
    listeners.set(chart, listener);
  },
  afterDestroy(chart) {
    const listener = listeners.get(chart);
    if (listener) document.removeEventListener('pointerdown', listener, true);
    listeners.delete(chart);
  },
};
