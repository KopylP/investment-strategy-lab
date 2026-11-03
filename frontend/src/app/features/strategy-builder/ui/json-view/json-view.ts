import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { toProblemMessage } from '../../../../core/api/api-client';
import { DraftStore } from '../../data-access/draft-store';
import { StrategyBuilderApi } from '../../data-access/strategy-builder-api';

interface JsonToken {
  text: string;
  kind: string;
}

@Component({
  selector: 'app-builder-json-view',
  imports: [Button],
  template: `<div class="mb-4 flex flex-wrap gap-2">
      <p-button label="Копіювати" icon="pi pi-copy" [disabled]="!json()" (onClick)="copy()" />
      <p-button
        label="Експортувати"
        icon="pi pi-download"
        severity="secondary"
        [disabled]="!json()"
        (onClick)="download()"
      />
      <p-button
        label="Імпортувати"
        icon="pi pi-upload"
        severity="secondary"
        [disabled]="importing()"
        (onClick)="file.click()"
      />
      <input
        #file
        type="file"
        accept=".json,application/json"
        class="hidden"
        (change)="importFile($event)"
      />
    </div>
    @if (loading()) {
      <p role="status">Готую JSON…</p>
    }
    @if (error()) {
      <p role="alert" class="mb-3 whitespace-pre-wrap text-red-500">{{ error() }}</p>
    }
    <pre
      class="max-h-[70vh] overflow-auto rounded border border-surface p-4 text-sm"
      aria-label="JSON правил"
    ><code>@for (token of tokens(); track $index) {<span [class.text-blue-500]="token.kind === 'key'" [class.text-green-600]="token.kind === 'string'" [class.text-amber-600]="token.kind === 'literal'">{{ token.text }}</span>}</code></pre>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JsonView {
  private readonly store = inject(DraftStore);
  private readonly api = inject(StrategyBuilderApi);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);
  protected readonly json = signal('');
  protected readonly tokens = signal<JsonToken[]>([]);
  protected readonly loading = signal(true);
  protected readonly importing = signal(false);
  protected readonly error = signal('');

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    const rules = await this.store.rules();
    if (rules) {
      const formatted = JSON.stringify(JSON.parse(rules.json), null, 2);
      this.json.set(formatted);
      this.tokens.set(colourise(formatted));
    } else this.error.set('Не вдалося отримати JSON. Перевірте проблеми чернетки.');
    this.loading.set(false);
  }

  protected async copy(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.json());
      this.messages.add({ severity: 'success', summary: 'JSON скопійовано' });
    } catch {
      this.error.set('Не вдалося скопіювати JSON. Скористайтеся експортом.');
    }
  }

  protected download(): void {
    const blob = new Blob([this.json()], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${this.store.draft()?.name ?? 'Стратегія'}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  protected async importFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.importing.set(true);
    this.error.set('');
    try {
      await this.store.flush();
      const name = file.name.replace(/\.json$/i, '') || 'Імпортована стратегія';
      const draft = await this.api.importRules(name, await file.text());
      await this.router.navigate(['/strategy-builder', draft.id]);
    } catch (error) {
      this.error.set(toProblemMessage(error));
    } finally {
      this.importing.set(false);
    }
  }
}

function colourise(json: string): JsonToken[] {
  const pattern = /("(?:\\.|[^"\\])*"\s*:?)|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|true|false|null)/g;
  const tokens: JsonToken[] = [];
  let offset = 0;
  for (const match of json.matchAll(pattern)) {
    const index = match.index;
    if (index > offset) tokens.push({ text: json.slice(offset, index), kind: 'plain' });
    const text = match[0];
    const kind = match[1] ? (text.endsWith(':') ? 'key' : 'string') : 'literal';
    tokens.push({ text, kind });
    offset = index + text.length;
  }
  if (offset < json.length) tokens.push({ text: json.slice(offset), kind: 'plain' });
  return tokens;
}
