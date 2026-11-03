import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { DraftStore } from '../../data-access/draft-store';
import { conditionRows } from '../../data-access/condition-rows-mapper';
import { graphWithNode } from '../../data-access/graph-mapper';
import { NodeDto } from '../../models/draft-dto';
import { blocks } from '../../models/draft-vm';
import { ConditionRows } from './condition-rows';
import { ReadonlyCondition } from './readonly-condition';
import { Inspector } from '../inspector/inspector';
import { SetupDialog } from '../setup-dialog/setup-dialog';
import { ReportsDialog } from '../reports-dialog/reports-dialog';
import { ProblemsPanel } from '../problems-panel/problems-panel';

@Component({
  selector: 'app-builder-steps-view',
  imports: [
    FormsModule,
    Button,
    InputText,
    SetupDialog,
    ReportsDialog,
    ConditionRows,
    ReadonlyCondition,
    Inspector,
    ProblemsPanel,
  ],
  templateUrl: './steps-view.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepsView {
  protected readonly store = inject(DraftStore);
  readonly editSetup = output<void>();
  readonly editReports = output<void>();
  protected readonly step = signal(0);
  protected readonly titles = [
    'Назва й опис',
    'Фонди й ряди',
    ...blocks.map((block) => block.title),
    'Рапорти',
    'Перевірка й бектест',
  ];
  protected readonly block = computed(() => blocks[this.step() - 2]?.kind ?? null);
  protected readonly rows = computed(() => {
    const draft = this.store.draft();
    const block = this.block();
    return draft && block ? conditionRows(draft.graph, block) : null;
  });
  protected readonly result = computed(() =>
    this.store
      .draft()
      ?.graph.nodes.find((node) => node.block === this.block() && node.kind.endsWith('Result')),
  );
  protected readonly sentence = computed(
    () =>
      this.store.draft()?.words.find((words) => words.block === this.block())?.sentence ??
      'Не налаштовано',
  );

  constructor() {
    const selected = this.store.selectedNodeId();
    const node = this.store.draft()?.graph.nodes.find((node) => node.id === selected);
    const index = blocks.findIndex((block) => block.kind === node?.block);
    if (index >= 0) this.step.set(index + 2);
  }

  protected updateNode(node: NodeDto): void {
    const draft = this.store.draft();
    if (draft) this.store.changeGraph(graphWithNode(draft.graph, node));
    this.store.select(node.id);
  }

  protected async editGraph(): Promise<void> {
    const draft = this.store.draft();
    const block = this.block();
    if (draft && block) {
      this.store.moveNodes({
        ...draft.layout,
        collapsed: draft.layout.collapsed.filter((kind) => kind !== block),
      });
    }
    await this.store.setMode('graph');
  }
}
