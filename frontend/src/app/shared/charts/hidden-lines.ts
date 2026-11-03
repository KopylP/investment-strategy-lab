import { signal } from '@angular/core';
import { LegendDataset } from './legend-dataset';
import { LegendLine } from './legend-line';

export class HiddenLines {
  private readonly hidden = signal<ReadonlySet<string>>(new Set());

  toggle(label: string): void {
    this.hidden.update((hidden) => {
      const next = new Set(hidden);

      if (next.has(label)) next.delete(label);
      else next.add(label);

      return next;
    });
  }

  applyTo<T extends LegendDataset>(datasets: T[]): (T & { hidden: boolean })[] {
    const hidden = this.hidden();

    return datasets.map((dataset) => ({ ...dataset, hidden: hidden.has(dataset.label) }));
  }

  legendOf(datasets: LegendDataset[]): LegendLine[] {
    const hidden = this.hidden();

    return datasets.map((dataset) => ({
      label: dataset.label,
      color: markerColorOf(dataset),
      shown: !hidden.has(dataset.label),
    }));
  }
}

function markerColorOf(dataset: LegendDataset): string {
  const color = dataset.showLine === false ? dataset.backgroundColor : dataset.borderColor;

  return typeof color === 'string' ? color : '';
}
