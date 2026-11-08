import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { DraftStore } from '../../data-access/draft-store';
import { Viewport } from '../../../../shared/ui/viewport';
import { resultsUsing } from '../../data-access/shared-graph';
import { candidatePath } from '../../data-access/portfolio-flow';
import { conditionRows } from '../../data-access/condition-rows-mapper';
import { graphWithNode } from '../../data-access/graph-mapper';
import { DraftProblemDto, NodeDto } from '../../models/draft-dto';
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
    TranslocoPipe,
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
  protected readonly viewport = inject(Viewport);
  readonly chooseProblem = output<DraftProblemDto>();
  readonly editSetup = output<void>();
  protected readonly guidance = [
    translate('builder.steps.hint.name'),
    translate('builder.steps.hint.setup'),
    translate('builder.steps.hint.market'),
    translate('builder.steps.hint.exit'),
    translate('builder.steps.hint.entry'),
    translate('builder.steps.hint.selection'),
    translate('builder.steps.hint.risk'),
    translate('builder.steps.hint.reports'),
    translate('builder.steps.hint.check'),
  ];
  readonly editReports = output<void>();
  protected readonly step = signal(0);
  private readonly stepNav = viewChild.required<ElementRef<HTMLElement>>('stepNav');
  protected readonly titles = [
    translate('builder.steps.title.name'),
    translate('builder.graph.visual.setup'),
    ...blocks.map((block) => block.title),
    translate('routes.reports'),
    translate('builder.steps.title.check'),
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
  protected readonly selector = computed(() => {
    const graph = this.store.draft()?.graph;
    return graph ? candidatePath(graph).find((node) => node.kind === 'FundSelection') : undefined;
  });
  protected readonly sentence = computed(
    () =>
      this.store.draft()?.words.find((words) => words.block === this.block())?.sentence ??
      translate('builder.words.notSet'),
  );

  constructor() {
    this.showNode(this.store.selectedNodeId());
    effect(() => this.revealStep(this.stepNav().nativeElement, this.step()));
  }

  private revealStep(nav: HTMLElement, step: number): void {
    const chip = nav.children.item(step) as HTMLElement | null;
    if (!chip) return;

    const chipStart = chip.getBoundingClientRect().left - nav.getBoundingClientRect().left;
    const centred = nav.scrollLeft + chipStart - (nav.clientWidth - chip.offsetWidth) / 2;
    nav.scrollTo({ left: centred, behavior: 'smooth' });
  }

  showNode(nodeId: string | null): void {
    const draft = this.store.draft();
    const node = draft?.graph.nodes.find((candidate) => candidate.id === nodeId);
    const context = node && draft ? resultsUsing(draft.graph, node.id)[0]?.block : node?.block;
    const index = blocks.findIndex((block) => block.kind === context);
    if (index >= 0) this.step.set(index + 2);
  }

  showBlock(kind: string): void {
    const index = blocks.findIndex((block) => block.kind === kind);
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
