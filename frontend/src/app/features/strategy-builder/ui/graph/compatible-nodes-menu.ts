import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NodeKindDto } from '../../models/vocabulary-dto';

@Component({
  selector: 'app-compatible-nodes-menu',
  imports: [TranslocoPipe],
  template: `
    <div
      class="min-w-52 rounded-xl border border-surface bg-surface-0 p-2 shadow-xl dark:bg-surface-900"
    >
      <div class="px-2 py-1 text-xs font-medium text-muted-color">
        {{ 'builder.graph.menu.insert' | transloco }}
      </div>
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
        <div class="px-2 py-2 text-sm text-muted-color">
          {{ 'builder.graph.menu.noneCompatible' | transloco }}
        </div>
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
    Compare: translate('builder.graph.node.compare'),
    Between: translate('builder.graph.node.between'),
    Crossed: translate('builder.graph.node.crossed'),
    All: translate('builder.graph.node.all'),
    Any: translate('builder.graph.node.any'),
    Not: translate('builder.graph.node.not'),
    MarketResult: translate('builder.graph.menu.result.market'),
    ExitResult: translate('builder.graph.menu.result.exit'),
    EntryResult: translate('builder.graph.menu.result.entry'),
    SelectionResult: translate('builder.graph.menu.result.selection'),
    RiskResult: translate('builder.graph.menu.result.risk'),
  };
}
