import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { toProblemMessage } from '../../../core/api/api-client';
import { DraftDto } from '../models/draft-dto';
import { StrategyBuilderApi } from './strategy-builder-api';

@Injectable()
export class DraftsStore {
  private readonly api = inject(StrategyBuilderApi);
  private readonly messages = inject(MessageService);
  private readonly draftsResource = resource({ loader: () => this.api.drafts() });
  private readonly vocabularyResource = resource({ loader: () => this.api.vocabulary() });

  readonly drafts = computed(() => this.draftsResource.value() ?? []);
  readonly vocabulary = computed(() => this.vocabularyResource.value());
  readonly isLoading = this.draftsResource.isLoading;
  readonly loadError = this.draftsResource.error;
  readonly busy = signal<'create' | 'import' | 'discard' | null>(null);

  async create(name: string, template: string | null): Promise<DraftDto | null> {
    this.busy.set('create');

    try {
      const draft = await this.api.create(name, template);
      this.draftsResource.reload();

      return draft;
    } catch (error) {
      this.reportFailure('Не вдалося створити чернетку', error);

      return null;
    } finally {
      this.busy.set(null);
    }
  }

  async importRules(name: string, json: string): Promise<DraftDto | null> {
    this.busy.set('import');

    try {
      const draft = await this.api.importRules(name, json);
      this.draftsResource.reload();

      return draft;
    } catch (error) {
      this.reportFailure('Не вдалося імпортувати правила', error);

      return null;
    } finally {
      this.busy.set(null);
    }
  }

  async discard(id: string): Promise<boolean> {
    this.busy.set('discard');

    try {
      await this.api.discard(id);
      this.draftsResource.reload();
      this.messages.add({ severity: 'success', summary: 'Чернетку видалено' });

      return true;
    } catch (error) {
      this.reportFailure('Не вдалося видалити чернетку', error);

      return false;
    } finally {
      this.busy.set(null);
    }
  }

  private reportFailure(summary: string, error: unknown): void {
    this.messages.add({
      severity: 'error',
      summary,
      detail: toProblemMessage(error),
      life: 10000,
    });
  }
}
