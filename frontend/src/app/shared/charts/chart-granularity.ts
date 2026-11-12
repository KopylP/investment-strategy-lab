import { Injectable, signal } from '@angular/core';
import { Granularity } from './series-buckets';

const storageKey = 'chart.granularity';

@Injectable({ providedIn: 'root' })
export class ChartGranularity {
  readonly step = signal<Granularity>(readStoredStep());

  choose(step: Granularity): void {
    this.step.set(step);
    storeStep(step);
  }
}

function readStoredStep(): Granularity {
  try {
    return localStorage.getItem(storageKey) === 'week' ? 'week' : 'day';
  } catch {
    return 'day';
  }
}

function storeStep(step: Granularity): void {
  try {
    localStorage.setItem(storageKey, step);
  } catch {}
}
