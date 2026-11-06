import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { BlockWordsDto } from '../../models/draft-dto';
import { blocks } from '../../models/draft-vm';

@Component({
  selector: 'app-builder-words-panel',
  template: `<section class="card mb-0" aria-label="Стратегія словами" [class.compact]="compact()">
    <h2 class="mb-4 text-lg font-semibold">Стратегія словами</h2>
    @for (block of blocks; track block.kind) {
      <div class="word-block">
        <h3 class="mt-3 font-medium">{{ block.title }}</h3>
        <p class="mt-1 text-sm text-muted-color">{{ sentence(block.kind) }}</p>
      </div>
    }
  </section>`,
  styles: `
    .compact {
      padding: 0;
      margin: 0;
      background: transparent;
    }
    .compact h2 {
      font-size: 14px;
      line-height: 1.4;
      margin: 0 0 10px;
    }
    .compact h3 {
      font-size: 13px;
      line-height: 1.4;
      margin: 0 0 4px;
    }
    .compact p {
      font-size: 12px;
      line-height: 1.6;
      margin: 0;
    }
    .compact .word-block + .word-block {
      margin-top: 12px;
    }
    @media (min-width: 760px) {
      .compact {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 12px 24px;
      }
      .compact h2 {
        grid-column: 1 / -1;
        margin-bottom: 0;
      }
      .compact .word-block + .word-block {
        margin-top: 0;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordsPanel {
  readonly compact = input(false);
  readonly words = input.required<BlockWordsDto[]>();
  protected readonly blocks = blocks;

  protected sentence(block: string): string {
    return this.words().find((words) => words.block === block)?.sentence ?? 'Не налаштовано';
  }
}
