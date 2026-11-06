import { computed, DestroyRef, inject, Injectable, resource, signal } from '@angular/core';
import { ExperimentsApi } from './experiments-api';

const pollMilliseconds = 5000;

@Injectable()
export class ExperimentStore {
  private readonly api = inject(ExperimentsApi);
  private readonly currentId = signal<string | null>(null);
  readonly kind = signal<string | null>(null);
  readonly from = signal('');
  readonly to = signal('');
  readonly offset = signal(0);

  private readonly experimentResource = resource({
    params: () => this.currentId() ?? undefined,
    loader: ({ params }) => this.api.get(params),
  });
  readonly experiment = computed(() => this.experimentResource.hasValue() ? this.experimentResource.value() : null);
  readonly loading = this.experimentResource.isLoading;
  readonly error = this.experimentResource.error;
  readonly waiting = computed(() => this.experiment()?.status === 'Queued' || this.experiment()?.status === 'Running');

  private readonly journalQuery = computed(() => {
    const id = this.currentId();
    if (!id || this.experiment()?.status !== 'Succeeded') return undefined;

    return { id, offset: this.offset(), kind: this.kind(), from: this.from() || null, to: this.to() || null };
  });
  private readonly journalResource = resource({
    params: () => this.journalQuery(),
    loader: ({ params }) => this.api.journal(params.id, params),
  });
  readonly journal = computed(() => this.journalResource.hasValue() ? this.journalResource.value() : null);
  readonly journalLoading = this.journalResource.isLoading;
  readonly journalError = this.journalResource.error;

  constructor() {
    const timer = setInterval(() => {
      if (this.waiting()) this.experimentResource.reload();
    }, pollMilliseconds);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  show(id: string | null): void {
    if (this.currentId() === id) return;
    this.currentId.set(id);
    this.offset.set(0);
    this.kind.set(null);
    this.from.set('');
    this.to.set('');
  }

  filter(kind: string | null, from: string, to: string): void {
    this.kind.set(kind);
    this.from.set(from);
    this.to.set(to);
    this.offset.set(0);
  }
}
