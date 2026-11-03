import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { BlockWordsDto } from '../../models/draft-dto';
import { blocks } from '../../models/draft-vm';

@Component({
  selector: 'app-builder-words-panel',
  template: `<section class="card mb-0" aria-label="Стратегія словами">
    <h2 class="mb-4 text-lg font-semibold">Стратегія словами</h2>
    @for (block of blocks; track block.kind) {
      <h3 class="mt-3 font-medium">{{ block.title }}</h3>
      <p class="mt-1 text-sm text-muted-color">{{ sentence(block.kind) }}</p>
    }
  </section>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordsPanel {
  readonly words = input.required<BlockWordsDto[]>();
  protected readonly blocks = blocks;

  protected sentence(block: string): string {
    return this.words().find((words) => words.block === block)?.sentence ?? 'Не налаштовано';
  }
}
