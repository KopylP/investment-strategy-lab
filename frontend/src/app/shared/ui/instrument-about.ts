import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

/** The short part of a description: its first sentences up to this length, but at least one. */
const shortLength = 240;
const shortSentences = 2;

/**
 * What an instrument is, as Yahoo describes it (English): a couple of sentences, and the rest behind «Детальніше».
 * Nothing at all when there is no description.
 */
@Component({
  selector: 'app-instrument-about',
  template: `
    @if (parts(); as parts) {
      <div class="text-sm text-muted-color leading-relaxed" lang="en">
        {{ expanded() ? parts.full : parts.short }}
        @if (parts.more) {
          <button type="button" class="p-0 border-0 bg-transparent text-primary cursor-pointer whitespace-nowrap" lang="uk" (click)="expandedText.set(expanded() ? null : text() ?? null)">
            {{ expanded() ? 'Згорнути' : 'Детальніше' }}
          </button>
        }
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstrumentAbout {
  readonly text = input<string | null | undefined>(null);

  // Kept per text: the same component showing another instrument starts short again.
  protected readonly expandedText = signal<string | null>(null);
  protected readonly expanded = computed(() => !!this.text() && this.expandedText() === this.text());

  protected readonly parts = computed(() => {
    const full = this.text()?.trim();
    if (!full) return null;
    const sentences = splitSentences(full);
    let short = sentences[0];
    for (const sentence of sentences.slice(1, shortSentences)) {
      if ((short + sentence).length > shortLength) break;
      short += sentence;
    }
    short = short.trim();
    return { full, short, more: short.length < full.length };
  });
}

/** English sentences with their trailing spaces; "Inc. designs" stays one sentence (no capital after the dot). */
function splitSentences(text: string): string[] {
  if (typeof Intl.Segmenter === 'function')
    return [...new Intl.Segmenter('en', { granularity: 'sentence' }).segment(text)].map((part) => part.segment);
  return text.match(/[^.!?]+(?:[.!?]+\s*|$)/g) ?? [text];
}
