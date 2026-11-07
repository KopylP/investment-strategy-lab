import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { TrialRunDto } from '../models/trial-run-dto';
import { StrategyBuilderApi } from './strategy-builder-api';

const pollMilliseconds = 5000;

@Injectable()
export class TrialsStore {
  private readonly api = inject(StrategyBuilderApi);
  private readonly currentTrials = signal<TrialRunDto[]>([]);
  private readonly chosen = signal<number[]>([]);
  private draftId: string | null = null;
  private requestNumber = 0;

  readonly trials = this.currentTrials.asReadonly();
  readonly loading = signal(false);
  readonly error = signal<unknown>(null);
  readonly selectedRuns = computed(() =>
    this.trials().filter((run) => this.chosen().includes(run.number) && this.canSelect(run)),
  );
  readonly selected = computed(() => this.selectedRuns().map((run) => run.number));
  readonly waiting = computed(() =>
    this.trials().some((run) => run.status === 'Waiting' || run.status === 'Running'),
  );

  constructor() {
    const timer = setInterval(() => {
      if (this.waiting()) void this.reload();
    }, pollMilliseconds);
    inject(DestroyRef).onDestroy(() => {
      clearInterval(timer);
      this.requestNumber++;
    });
  }

  show(id: string, run: number | null): void {
    this.requestNumber++;
    this.draftId = id;
    this.currentTrials.set([]);
    this.chosen.set(run === null ? [] : [run]);
    this.loading.set(false);
    void this.reload();
  }

  canSelect(run: TrialRunDto): boolean {
    return run.status !== 'Failed' && run.status !== 'Cancelled';
  }

  toggle(number: number): void {
    const run = this.trials().find((trial) => trial.number === number);
    if (!run || !this.canSelect(run)) return;
    const selected = this.selected();
    if (selected.includes(number)) {
      this.chosen.set(selected.filter((current) => current !== number));
      return;
    }
    const canCompare =
      run.status === 'Succeeded' &&
      this.selectedRuns().every((current) => current.status === 'Succeeded');
    this.chosen.set(canCompare ? [...selected.slice(-1), number] : [number]);
  }

  private async reload(): Promise<void> {
    if (!this.draftId || this.loading()) return;
    const request = ++this.requestNumber;
    this.loading.set(true);

    try {
      const trials = await this.api.trials(this.draftId);
      if (request !== this.requestNumber) return;

      this.currentTrials.set(trials);
      this.error.set(null);
    } catch (error) {
      if (request === this.requestNumber) this.error.set(error);
    } finally {
      if (request === this.requestNumber) this.loading.set(false);
    }
  }
}
