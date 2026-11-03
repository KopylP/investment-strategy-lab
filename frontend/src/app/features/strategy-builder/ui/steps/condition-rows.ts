import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { OwnSignalDto } from '../../models/own-signal-dto';
import { GraphDto, NodeDto } from '../../models/draft-dto';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { VocabularyDto } from '../../models/vocabulary-dto';
import { ConditionRows as Rows } from '../../data-access/condition-rows-mapper';
import { Inspector } from '../inspector/inspector';

@Component({
  selector: 'app-condition-rows',
  imports: [Inspector],
  template: `<div class="flex flex-col gap-3">
    @if (rows().junction; as junction) {
      <p class="font-medium">
        {{ junction.kind === 'All' ? 'Усі умови (І)' : 'Будь-яка умова (АБО)' }}
      </p>
    }
    @for (node of editableNodes(); track node.id) {
      <div (focusin)="select.emit(node.id)" (click)="select.emit(node.id)">
        <app-node-inspector
          [node]="node"
          [graph]="graph()"
          [vocabulary]="vocabulary()"
          [instruments]="instruments()"
          [signals]="signals()"
          [allowDuplicate]="false"
          (nodeChange)="nodeChange.emit($event)"
        />
      </div>
    }
  </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConditionRows {
  readonly rows = input.required<Rows>();
  readonly graph = input.required<GraphDto>();
  readonly vocabulary = input.required<VocabularyDto>();
  readonly instruments = input.required<InstrumentChoiceDto[]>();
  readonly signals = input<OwnSignalDto[]>([]);
  readonly nodeChange = output<NodeDto>();
  readonly select = output<string>();
  protected readonly editableNodes = computed(() => {
    const conditions = this.rows().conditions;
    const ids = new Set(conditions.map((node) => node.id));
    const sources = this.graph()
      .links.filter((link) => ids.has(link.to))
      .map((link) => link.from);
    return this.graph().nodes.filter((node) => ids.has(node.id) || sources.includes(node.id));
  });
}
