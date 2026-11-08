import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  resource,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { toProblemMessage } from '../../../../core/api/api-client';
import { StrategyBuilderApi } from '../../data-access/strategy-builder-api';
import { WordsPanel } from '../words-panel/words-panel';

@Component({
  selector: 'app-published-strategy-panel',
  imports: [TranslocoPipe, Button, Message, WordsPanel],
  templateUrl: './published-strategy-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublishedStrategyPanel {
  readonly strategyId = input.required<string>();
  readonly version = input.required<number>();
  readonly editable = output<boolean>();
  private readonly api = inject(StrategyBuilderApi);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);
  private readonly publishedResource = resource({
    params: () => ({ id: this.strategyId(), version: this.version() }),
    loader: ({ params }) => this.api.published(params.id, params.version),
  });
  protected readonly published = computed(() =>
    this.publishedResource.hasValue() ? this.publishedResource.value() : null,
  );
  protected readonly error = this.publishedResource.error;
  protected readonly opening = signal(false);

  constructor() {
    effect(() => {
      const published = this.published();
      if (published) this.editable.emit(published.editable);
    });
  }

  protected async open(): Promise<void> {
    if (this.opening()) return;
    this.opening.set(true);

    try {
      const draft = await this.api.openPublished(this.strategyId(), this.version());
      await this.router.navigate(['/strategy-builder', draft.id]);
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: translate('builder.published.openFailed'),
        detail: toProblemMessage(error),
      });
    } finally {
      this.opening.set(false);
    }
  }
}
