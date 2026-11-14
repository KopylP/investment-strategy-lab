import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  HostListener,
  ElementRef,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LayoutService } from '../../../../core/layout/layout-service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Menubar } from 'primeng/menubar';
import { Message } from 'primeng/message';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { SplitButton } from 'primeng/splitbutton';
import { Tooltip } from 'primeng/tooltip';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { Viewport } from '../../../../shared/ui/viewport';
import { SaveStatus, saveStatuses } from '../../models/save-status-vm';
import { DraftStore } from '../../data-access/draft-store';
import { setupSummary } from '../../data-access/draft-mapper';
import { graphWithNode } from '../../data-access/graph-mapper';
import { DraftProblemDto, NodeDto } from '../../models/draft-dto';
import { EditorMode } from '../../models/draft-vm';
import { confirmDiscardDraft } from '../../ui/confirm-discard-draft';
import { ReportsDialog } from '../../ui/reports-dialog/reports-dialog';
import { GraphEditor } from '../../ui/graph/graph-editor';
import { SetupDialog } from '../../ui/setup-dialog/setup-dialog';
import { SetupRow } from '../../ui/setup-row/setup-row';
import { NewPaletteNode } from '../../ui/palette/palette';
import { CopyEntryDialog } from '../../ui/graph/copy-entry-dialog';
import { ExperimentDialog } from '../../ui/experiment-dialog/experiment-dialog';
import { ExperimentRequestVm } from '../../models/experiment-request-vm';
import { JsonValue } from '../../../../shared/models/json-value';
import { experimentParameters } from '../../data-access/experiment-parameters-mapper';

import { JsonView } from '../../ui/json-view/json-view';
import { StepsView } from '../../ui/steps/steps-view';
import { WordsPanel } from '../../ui/words-panel/words-panel';
import { ProblemsPanel } from '../../ui/problems-panel/problems-panel';

@Component({
  selector: 'app-builder-draft-editor-page',
  imports: [
    TranslocoPipe,
    RouterLink,
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
    SplitButton,
    Tooltip,
    Menu,
    Menubar,
    PageHeader,
    ReportsDialog,
    GraphEditor,
    SetupDialog,
    SetupRow,
    CopyEntryDialog,
    ExperimentDialog,
  ],
  providers: [DraftStore],
  templateUrl: './draft-editor-page.html',
  styleUrl: './draft-editor-page.css',
  host: { '[class.graph-mode]': "store.mode() === 'graph'" },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftEditorPage {
  protected readonly store = inject(DraftStore);
  private readonly graphEditor = viewChild(GraphEditor);
  private readonly stepsView = viewChild(StepsView);
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  protected readonly viewport = inject(Viewport);
  private readonly messages = inject(MessageService);
  protected readonly saveStatuses = saveStatuses;

  protected closeReview(): void {
    this.store.reviewVisible.set(false);
    requestAnimationFrame(() =>
      this.element.nativeElement.querySelector<HTMLButtonElement>('.review-button')?.focus(),
    );
  }
  @HostListener('document:keydown.escape')
  protected dismissReview(): void {
    if (
      this.store.reviewVisible() &&
      !this.reportsVisible() &&
      !this.setupVisible() &&
      !this.copyEntryVisible() &&
      !document.querySelector('[role="dialog"], [role="listbox"]')
    )
      this.closeReview();
  }
  @HostListener('document:keydown', ['$event'])
  protected saveOnShortcut(event: KeyboardEvent): void {
    const saveShortcut = (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 's';
    if (!saveShortcut) return;

    event.preventDefault();
    void this.save();
  }
  private readonly router = inject(Router);
  private readonly appLayout = inject(LayoutService);
  private readonly confirmation = inject(ConfirmationService);
  readonly draftId = input.required<string>();
  protected readonly setupVisible = signal(false);
  protected readonly reportsVisible = signal(false);
  protected readonly copyEntryVisible = signal(false);
  protected readonly experimentVisible = signal(false);
  private readonly experimentRules = signal<Record<string, JsonValue>>({});
  protected readonly experimentChoices = computed(() => {
    const vocabulary = this.store.vocabulary();
    return vocabulary ? experimentParameters(this.experimentRules(), vocabulary) : [];
  });
  private readonly allModes: { label: string; value: EditorMode }[] = [
    { label: translate('builder.editor.steps'), value: 'steps' },
    { label: translate('builder.editor.diagram'), value: 'graph' },
    { label: 'JSON', value: 'json' },
  ];
  protected readonly modes = computed(() =>
    this.viewport.phone() ? this.allModes.filter((mode) => mode.value !== 'graph') : this.allModes,
  );
  protected readonly saveStatus = computed<SaveStatus>(() => {
    if (this.store.saveError()) return 'failed';

    return this.store.saving() === 'saved' ? 'saved' : 'saving';
  });
  protected readonly problemsLabel = computed(() => {
    const count = this.store.draft()?.problems.length ?? 0;

    return count
      ? translate('builder.editor.remarks', { problems: count })
      : translate('builder.editor.noRemarks');
  });
  protected readonly saveMenu: MenuItem[] = [
    {
      label: translate('builder.editor.publishMenu'),
      icon: 'pi pi-check-circle',
      command: () => void this.confirmPublish(),
    },
  ];
  protected readonly menu = computed<MenuItem[]>(() => [
    this.strategyMenu(),
    this.editMenu(),
    this.viewMenu(),
    this.backtestMenu(),
    this.helpMenu(),
  ]);
  protected readonly phoneMenuItems = computed<MenuItem[]>(() => [
    {
      label: translate('builder.editor.backtestMenu'),
      icon: 'pi pi-play',
      disabled: this.store.busy(),
      command: () => void this.runTrial(),
    },
    {
      label: translate('builder.editor.trialHistory'),
      icon: 'pi pi-history',
      command: () => this.openTrials(),
    },
    { separator: true },
    {
      label: translate('builder.editor.reportsMenu'),
      icon: 'pi pi-file',
      command: () => this.reportsVisible.set(true),
    },
    {
      label: translate('builder.editor.describeWithCount', {
        count: this.store.draft()?.problems.length ?? 0,
      }),
      icon: 'pi pi-check-circle',
      command: () => this.store.reviewVisible.set(true),
    },
    { separator: true },
    {
      label: translate('builder.editor.deleteMenu'),
      icon: 'pi pi-trash',
      styleClass: 'danger-item',
      command: () => this.confirmDiscard(),
    },
  ]);
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
    effect(() => {
      if (this.viewport.phone() && this.store.mode() === 'graph') void this.store.setMode('steps');
    });
    effect((onCleanup) => {
      if (this.store.mode() !== 'graph') return;
      const previousMode = untracked(() => this.appLayout.layoutConfig().menuMode);
      this.appLayout.layoutConfig.update((config) => ({ ...config, menuMode: 'overlay' }));
      onCleanup(() => {
        this.appLayout.layoutConfig.update((config) => ({ ...config, menuMode: previousMode }));
        this.appLayout.layoutState.update((state) => ({ ...state, overlayMenuActive: false }));
      });
    });
  }

  protected nameWidth(name: string): number {
    return Math.min(Math.max(name.length + 2, 12), 48);
  }

  protected async save(): Promise<void> {
    await this.store.flush();
    if (this.store.saveError()) return;

    this.messages.add({
      severity: 'success',
      summary: translate('builder.editor.saved'),
      life: 2000,
    });
  }

  private openTrials(): void {
    void this.router.navigate(['/strategy-builder', this.draftId(), 'trials']);
  }

  private openSetup(): void {
    if (this.store.mode() === 'graph') void this.graphEditor()?.focusSetup();
    else this.setupVisible.set(true);
  }

  private strategyMenu(): MenuItem {
    return {
      label: translate('routes.strategy'),
      items: [
        {
          label: translate('builder.editor.setupMenu'),
          icon: 'pi pi-sliders-h',
          command: () => this.openSetup(),
        },
        {
          label: translate('builder.editor.reportsMenu'),
          icon: 'pi pi-file',
          command: () => this.reportsVisible.set(true),
        },
        {
          label: translate('builder.editor.copyEntryMenu'),
          icon: 'pi pi-copy',
          command: () => this.copyEntryVisible.set(true),
        },
        { separator: true },
        {
          label: translate('common.save'),
          icon: 'pi pi-save',
          title: '⌘S / Ctrl+S',
          command: () => void this.save(),
        },
        {
          label: translate('builder.editor.publishMenu'),
          icon: 'pi pi-check-circle',
          disabled: this.store.busy(),
          command: () => void this.confirmPublish(),
        },
        { separator: true },
        {
          label: translate('builder.editor.deleteMenu'),
          icon: 'pi pi-trash',
          styleClass: 'danger-item',
          disabled: this.store.busy(),
          command: () => this.confirmDiscard(),
        },
      ],
    };
  }

  private editMenu(): MenuItem {
    const graph = this.graphEditor();

    return {
      label: translate('builder.editor.edit'),
      visible: !!graph,
      items: [
        {
          label: translate('builder.editor.undo'),
          icon: 'pi pi-undo',
          disabled: !graph?.canUndo(),
          command: () => graph?.undo(),
        },
        {
          label: translate('builder.editor.redo'),
          icon: 'pi pi-refresh',
          disabled: !graph?.canRedo(),
          command: () => graph?.redo(),
        },
        { separator: true },
        {
          label: translate('builder.editor.arrangeDiagram'),
          icon: 'pi pi-sitemap',
          command: () => void graph?.arrangeAll(),
        },
      ],
    };
  }

  private viewMenu(): MenuItem {
    const graph = this.graphEditor();
    const modeItems = this.allModes.map((mode) => ({
      label: mode.label,
      icon: this.store.mode() === mode.value ? 'pi pi-check' : 'pi pi-fw',
      command: () => this.changeMode(mode.value),
    }));
    const graphItems: MenuItem[] = graph
      ? [
          { separator: true },
          {
            label: translate('builder.graph.editor.fitAll'),
            icon: 'pi pi-arrows-alt',
            command: () => void graph.fit(),
          },
          {
            label: translate('builder.graph.editor.zoomIn'),
            icon: 'pi pi-search-plus',
            command: () => void graph.setZoom(1),
          },
          {
            label: translate('builder.graph.editor.zoomOut'),
            icon: 'pi pi-search-minus',
            command: () => void graph.setZoom(-1),
          },
          {
            label: translate('builder.graph.editor.fullscreen'),
            icon: 'pi pi-expand',
            command: () => void graph.toggleFullscreen(),
          },
        ]
      : [];

    return {
      label: translate('builder.editor.view'),
      items: [
        ...modeItems,
        ...graphItems,
        { separator: true },
        {
          label: translate('builder.editor.describeShort'),
          icon: 'pi pi-check-circle',
          command: () => this.store.reviewVisible.set(true),
        },
      ],
    };
  }

  private backtestMenu(): MenuItem {
    return {
      label: translate('builder.editor.backtest'),
      items: [
        {
          label: translate('builder.editor.runBacktest'),
          icon: 'pi pi-play',
          disabled: this.store.busy(),
          command: () => void this.runTrial(),
        },
        {
          label: translate('builder.editor.trialHistory'),
          icon: 'pi pi-history',
          command: () => this.openTrials(),
        },
      ],
    };
  }

  private helpMenu(): MenuItem {
    const graph = this.graphEditor();

    return {
      label: translate('builder.editor.help'),
      visible: !!graph,
      items: [
        {
          label: translate('builder.graph.editor.help'),
          icon: 'pi pi-question-circle',
          command: () => graph?.helpVisible.set(true),
        },
      ],
    };
  }

  protected confirmDiscard(): void {
    const draft = this.store.draft();
    if (!draft || this.store.busy()) return;
    confirmDiscardDraft(this.confirmation, draft.name, () => void this.discard());
  }
  private async discard(): Promise<void> {
    if (await this.store.discard()) await this.router.navigate(['/strategy-builder']);
  }

  protected async runTrial(): Promise<void> {
    if (!(await this.store.check())) return;
    const rules = await this.store.rules();
    if (!rules) return;
    this.experimentRules.set(JSON.parse(rules.json));
    this.store.trialError.set(null);
    this.experimentVisible.set(true);
  }

  protected async launchExperiment(request: ExperimentRequestVm): Promise<void> {
    const run = await this.store.runTrial(request.experiment, request.label);
    if (run) {
      this.experimentVisible.set(false);
      await this.router.navigate(['/strategy-builder', this.draftId(), 'trials'], {
        queryParams: { run: run.number },
      });
    }
  }

  protected async confirmPublish(): Promise<void> {
    if (!(await this.store.check())) return;
    const draft = this.store.draft();
    if (!draft) return;

    const message = draft.originStrategyId
      ? translate('builder.editor.publishVersion', {
          version: (draft.originVersion ?? 0) + 1,
          name: draft.name,
        })
      : translate('builder.editor.publishNew', { name: draft.name });
    this.confirmation.confirm({
      header: translate('builder.editor.publishTitle'),
      message,
      acceptLabel: translate('builder.editor.publish'),
      rejectLabel: translate('common.cancel'),
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

    if (!problem.nodeId && !problem.block) {
      this.setupVisible.set(true);
      return;
    }

    if (this.viewport.phone()) {
      this.showProblemInSteps(problem);
      return;
    }

    await this.store.setMode('graph');
    const draft = this.store.draft();
    if (!draft) return;

    const block =
      problem.block ?? draft.graph.nodes.find((node) => node.id === problem.nodeId)?.block;
    if (block && block !== 'Shared' && draft.layout.collapsed.includes(block)) {
      this.store.moveNodes({
        ...draft.layout,
        collapsed: draft.layout.collapsed.filter((kind) => kind !== block),
      });
    }
    this.store.select(problem.nodeId);
  }

  private showProblemInSteps(problem: DraftProblemDto): void {
    const steps = this.stepsView();
    if (problem.nodeId) steps?.showNode(problem.nodeId);
    else if (problem.block) steps?.showBlock(problem.block);
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

  protected removeNode(): void {
    const draft = this.store.draft();
    const node = this.selectedNode();
    if (!draft || !node || node.kind.endsWith('Result')) return;
    this.store.changeGraph({
      nodes: draft.graph.nodes.filter((item) => item.id !== node.id),
      links: draft.graph.links.filter((link) => link.from !== node.id && link.to !== node.id),
    });
    this.store.select(null);
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
