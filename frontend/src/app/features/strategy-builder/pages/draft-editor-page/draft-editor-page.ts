import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { DraftStore } from '../../data-access/draft-store';
import { setupSummary } from '../../data-access/draft-mapper';
import { graphWithNode } from '../../data-access/graph-mapper';
import { DraftProblemDto, NodeDto } from '../../models/draft-dto';
import { EditorMode } from '../../models/draft-vm';
import { ReportsDialog } from '../../ui/reports-dialog/reports-dialog';
import { GraphEditor } from '../../ui/graph/graph-editor';
import { SetupDialog } from '../../ui/setup-dialog/setup-dialog';
import { SetupRow } from '../../ui/setup-row/setup-row';
import { Inspector } from '../../ui/inspector/inspector';
import { NewPaletteNode, Palette } from '../../ui/palette/palette';
import { CopyEntryDialog } from '../../ui/graph/copy-entry-dialog';

import { JsonView } from '../../ui/json-view/json-view';
import { StepsView } from '../../ui/steps/steps-view';
import { WordsPanel } from '../../ui/words-panel/words-panel';
import { ProblemsPanel } from '../../ui/problems-panel/problems-panel';

@Component({
  selector: 'app-builder-draft-editor-page',
  imports: [
    JsonView,
    StepsView,
    WordsPanel,
    ProblemsPanel,
    FormsModule,
    Button,
    InputText,
    Message,
    SelectButton,
    Skeleton,
    Tooltip,
    PageHeader,
    ReportsDialog,
    GraphEditor,
    SetupDialog,
    SetupRow,
    Inspector,
    Palette,
    CopyEntryDialog,
  ],
  providers: [DraftStore],
  templateUrl: './draft-editor-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftEditorPage {
  protected readonly store = inject(DraftStore);
  private readonly router = inject(Router);
  private readonly confirmation = inject(ConfirmationService);
  readonly draftId = input.required<string>();
  protected readonly setupVisible = signal(false);
  protected readonly reportsVisible = signal(false);
  protected readonly copyEntryVisible = signal(false);
  protected readonly modes: { label: string; value: EditorMode }[] = [
    { label: 'Кроки', value: 'steps' },
    { label: 'Схема', value: 'graph' },
    { label: 'JSON', value: 'json' },
  ];
  protected readonly summary = computed(() => {
    const draft = this.store.draft();
    if (!draft) return null;

    return setupSummary(draft, this.store.instruments(), this.store.reportNames());
  });
  protected readonly selectedNode = computed(() => {
    const draft = this.store.draft();
    const selected = this.store.selectedNodeId();

    return draft?.graph.nodes.find((node) => node.id === selected) ?? null;
  });

  constructor() {
    effect(() => void this.store.open(this.draftId()));
  }

  protected async runTrial(): Promise<void> {
    const run = await this.store.runTrial();
    if (run)
      await this.router.navigate(['/strategy-builder', this.draftId(), 'trials'], {
        queryParams: { run: run.number },
      });
  }

  protected async confirmPublish(): Promise<void> {
    if (!(await this.store.check())) return;
    const draft = this.store.draft();
    if (!draft) return;

    const message = draft.originStrategyId
      ? `Опублікувати версію ${(draft.originVersion ?? 0) + 1} стратегії «${draft.name}»?`
      : `Опублікувати як нову стратегію «${draft.name}»?`;
    this.confirmation.confirm({
      header: 'Публікація стратегії',
      message,
      acceptLabel: 'Опублікувати',
      rejectLabel: 'Скасувати',
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.publish(),
    });
  }

  private async publish(): Promise<void> {
    const published = await this.store.publish();
    if (published)
      await this.router.navigate(['/strategies', published.strategyId], { replaceUrl: true });
  }

  protected async showProblem(problem: DraftProblemDto): Promise<void> {
    if (problem.reportCode) {
      this.reportsVisible.set(true);
      return;
    }

    await this.store.setMode('graph');
    const draft = this.store.draft();
    if (!draft) return;

    const block =
      problem.block ?? draft.graph.nodes.find((node) => node.id === problem.nodeId)?.block;
    if (block && draft.layout.collapsed.includes(block)) {
      this.store.moveNodes({
        ...draft.layout,
        collapsed: draft.layout.collapsed.filter((kind) => kind !== block),
      });
    }
    this.store.select(problem.nodeId);
  }

  protected changeMode(mode: EditorMode): void {
    void this.store.setMode(mode);
  }

  protected updateNode(node: NodeDto): void {
    const draft = this.store.draft();
    if (draft) this.store.changeGraph(graphWithNode(draft.graph, node));
  }

  protected addNode(request: NewPaletteNode): void {
    const draft = this.store.draft();
    if (!draft) return;

    const id = this.nextNodeId(request.kind.toLowerCase(), draft.graph.nodes);
    const node: NodeDto = { id, ...request };
    this.store.changeGraph({ ...draft.graph, nodes: [...draft.graph.nodes, node] });
    this.store.select(id);
  }

  protected duplicateNode(): void {
    const draft = this.store.draft();
    const selected = this.selectedNode();
    if (!draft || !selected || selected.kind.endsWith('Result')) return;

    const id = this.nextNodeId(`${selected.id}-copy`, draft.graph.nodes);
    const node: NodeDto = { ...selected, id, parameters: structuredClone(selected.parameters) };
    const position = draft.layout.positions[selected.id];
    const positions = position
      ? { ...draft.layout.positions, [id]: { x: position.x + 32, y: position.y + 32 } }
      : draft.layout.positions;

    this.store.changeGraph({ ...draft.graph, nodes: [...draft.graph.nodes, node] });
    this.store.moveNodes({ ...draft.layout, positions });
    this.store.select(id);
  }

  protected async copyEntryToExit(margin: number): Promise<void> {
    if (await this.store.copyEntryToExit(margin)) this.copyEntryVisible.set(false);
  }

  private nextNodeId(prefix: string, nodes: NodeDto[]): string {
    const used = new Set(nodes.map((node) => node.id));
    if (!used.has(prefix)) return prefix;

    let number = 2;
    while (used.has(`${prefix}-${number}`)) number++;

    return `${prefix}-${number}`;
  }
}
