import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NodeKindDto } from '../../models/vocabulary-dto';

@Component({
  selector: 'app-compatible-nodes-menu',
  template: `
    <div
      class="min-w-52 rounded-xl border border-surface bg-surface-0 p-2 shadow-xl dark:bg-surface-900"
    >
      <div class="px-2 py-1 text-xs font-medium text-muted-color">Вставити вузол</div>
      @for (kind of kinds(); track kind.kind) {
        <button
          type="button"
          class="block w-full rounded-lg px-2 py-2 text-left text-sm hover:bg-surface-100 dark:hover:bg-surface-800"
          (click)="choose.emit(kind)"
        >
          {{ labels[kind.kind] }}
        </button>
      }
      @if (!kinds().length) {
        <div class="px-2 py-2 text-sm text-muted-color">Немає сумісних вузлів</div>
      }
    </div>
  `,
  host: { class: 'absolute z-30 block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompatibleNodesMenu {
  readonly kinds = input.required<NodeKindDto[]>();
  readonly choose = output<NodeKindDto>();
  protected readonly labels: Record<string, string> = {
    Compare: 'Порівняння',
    Between: 'Між межами',
    Crossed: 'Перетин рівня',
    All: 'Усі умови',
    Any: 'Будь-яка умова',
    Not: 'НЕ',
    MarketResult: 'Результат ринкового фільтра',
    ExitResult: 'Результат виходу',
    EntryResult: 'Результат входу',
    SelectionResult: 'Результат відбору',
    RiskResult: 'Результат ризику',
  };
}
