import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  Injector,
  OnDestroy,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { ClassicPreset, NodeEditor } from 'rete';
import { layoutGraph, portId, edgeId } from './graph-layout';
import {
  NodeBox,
  Point,
  roundedPath,
  routePoints,
  editablePoints,
  anchoredPoints,
  edgeHandles,
  retargetRoute,
  routeIsClear,
} from './edge-routing';
import { AreaPlugin, AreaExtensions } from 'rete-area-plugin';
import { getDOMSocketPosition } from 'rete-render-utils';
import { AngularPlugin, AngularArea2D, Presets } from 'rete-angular-plugin/21';
import { ConnectionPlugin, ClassicFlow, SocketData } from 'rete-connection-plugin';
import {
  BlockWordsDto,
  DraftProblemDto,
  DraftSetupDto,
  GraphDto,
  LayoutDto,
  NodeDto,
  NodePosition,
} from '../../models/draft-dto';
import { VocabularyDto } from '../../models/vocabulary-dto';
import { OwnSignalDto } from '../../models/own-signal-dto';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { cloneGraph, cloneLayout, snapPosition } from '../../data-access/graph-mapper';
import { NewPaletteNode, Palette } from '../palette/palette';
import { ReteNode } from './rete-node';
import { Schemes, StrategyNode } from './rete-model';
import { ReteConnection, ReteSocket } from './rete-visuals';
import { SetupNode } from './setup-node';
import { setupNodeId, withSetup, resolvedSeries } from '../../data-access/setup-bindings';
import { withPortfolioFlow, candidatePath } from '../../data-access/portfolio-flow';
import { wireStyles } from './graph-visuals';
import { blocks } from '../../models/draft-vm';
import {
  dependenciesOf,
  resultsUsing,
  fundContext,
  roleDependenciesOf,
} from '../../data-access/shared-graph';

interface Snapshot {
  graph: GraphDto;
  layout: LayoutDto;
  setup: DraftSetupDto;
}
type Extra = AngularArea2D<Schemes>;
@Component({
  selector: 'app-graph-editor',
  imports: [TranslocoPipe, Palette],
  templateUrl: './graph-editor.html',
  styleUrl: './graph-editor.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraphEditor implements AfterViewInit, OnDestroy {
  readonly setup = input.required<DraftSetupDto>();
  readonly setupChange = output<DraftSetupDto>();
  readonly instrumentsChanged = output<void>();
  readonly graph = input.required<GraphDto>();
  readonly layout = input.required<LayoutDto>();
  readonly vocabulary = input.required<VocabularyDto>();
  readonly problems = input.required<DraftProblemDto[]>();
  readonly selectedNodeId = input.required<string | null>();
  readonly words = input.required<BlockWordsDto[]>();
  readonly signals = input<OwnSignalDto[]>([]);
  readonly instruments = input<InstrumentChoiceDto[]>([]);
  readonly graphChange = output<GraphDto>();
  readonly layoutChange = output<LayoutDto>();
  readonly select = output<string | null>();
  private readonly workspace = viewChild.required<ElementRef<HTMLElement>>('workspace');
  readonly isFullscreen = signal(false);
  protected readonly overlayContainer = signal<HTMLElement | 'body'>('body');
  private readonly canvas = viewChild.required<ElementRef<HTMLElement>>('canvas');
  private readonly injector = inject(Injector);
  protected readonly zoom = signal(1);
  readonly helpVisible = signal(false);
  protected readonly wireLegend = Object.entries(wireStyles).map(([type, style]) => ({
    type,
    ...style,
  }));

  // Walk upstream and downstream independently: siblings sharing an input are not part of this branch.
  private branchNodes(graph: GraphDto, selected: string | null): Set<string> | null {
    if (!selected) return null;
    const related = new Set([selected]);
    for (const direction of ['upstream', 'downstream']) {
      const visited = new Set([selected]);
      const queue = [selected];
      while (queue.length) {
        const id = queue.pop()!;
        for (const link of graph.links) {
          const next =
            direction === 'upstream'
              ? link.to === id
                ? link.from
                : null
              : link.from === id
                ? link.to
                : null;
          if (next && !visited.has(next)) {
            visited.add(next);
            related.add(next);
            queue.push(next);
          }
        }
      }
    }
    return related;
  }
  protected readonly error = signal<string | null>(null);
  protected readonly connecting = signal(false);
  protected readonly paletteOpen = signal(false);
  readonly canUndo = signal(false);
  readonly canRedo = signal(false);
  private editor = new NodeEditor<Schemes>();
  private area?: AreaPlugin<Schemes, Extra>;
  private connection = new ConnectionPlugin<Schemes, Extra>();
  private ready = false;
  private syncing = false;
  private destroyed = false;
  private pending = Promise.resolve();
  private undoStack: Snapshot[] = [];
  private redoStack: Snapshot[] = [];
  private picked?: SocketData;
  private keyboardSource: string | null = null;
  private keyboardOutput = 'output';
  protected readonly roles = blocks;
  private observer?: ResizeObserver;
  private connectionHistoryPending = false;
  private connectionHistoryTimer?: ReturnType<typeof setTimeout>;
  private arrangedRoutes = new Map<string, Point[]>();
  private dragRoutes?: Map<string, Point[]>;
  protected readonly selectedEdge = signal<string | null>(null);
  private drawnPoints = new Map<string, Point[]>();
  private edgeDrag?: {
    id: string;
    key: string;
    segment: number;
    axis: 'x' | 'y';
    points: Point[];
    layout: LayoutDto;
  };
  private routeCache = new Map<
    string,
    { path: string; points: Point[]; manual: string; geometry: string }
  >();
  private routeFrame?: number;
  private graphValue: GraphDto = { nodes: [], links: [] };
  private layoutValue: LayoutDto = { positions: {}, collapsed: [] };

  constructor() {
    effect(() => {
      const graph = this.graph(),
        layout = this.layout(),
        vocabulary = this.vocabulary();
      const selected = this.selectedNodeId(),
        problems = this.problems();
      const signals = this.signals(),
        instruments = this.instruments();
      const setup = this.setup();
      if (this.ready)
        this.enqueue(() =>
          this.sync(graph, layout, vocabulary, selected, problems, signals, instruments, setup),
        );
    });
  }
  ngAfterViewInit() {
    const area = (this.area = new AreaPlugin<Schemes, Extra>(this.canvas().nativeElement));
    const render = new AngularPlugin<Schemes, Extra>({ injector: this.injector });
    render.addPreset(
      Presets.classic.setup({
        socketPositionWatcher: getDOMSocketPosition({ offset: (position) => position }),
        customize: {
          node: (context) => (context.payload.dto.kind === 'Setup' ? SetupNode : ReteNode),
          connection: () => ReteConnection,
          socket: () => ReteSocket,
        },
      }),
    );
    render.addPipe((context) => {
      if (context.type === 'connectionpath' && context.data.points.length === 2) {
        const connection = context.data.payload;
        if (this.editor.getConnection(connection.id))
          context.data.path = this.connectionPath(
            connection,
            context.data.points[0],
            context.data.points[1],
          );
      }
      return context;
    });
    this.editor.use(area);
    area.use(this.connection);
    area.use(render);
    this.connection.addPreset(
      () =>
        new ClassicFlow({
          canMakeConnection: (a, b) => {
            const source = a.side === 'output' ? a : b;
            const target = a.side === 'input' ? a : b;
            const reason = this.connectionProblem(
              source.nodeId,
              target.nodeId,
              target.key,
              source.key,
            );
            this.error.set(reason);
            return !reason && a.side !== b.side;
          },
        }),
    );
    this.connection.addPipe((context) => {
      if (context.type === 'connectionpick') {
        this.picked = context.data.socket;
        this.connecting.set(true);
        this.refreshPorts();
      }
      if (context.type === 'connectiondrop') {
        this.picked = undefined;
        this.connecting.set(false);
        this.refreshPorts();
      }
      return context;
    });
    this.editor.addPipe((context) => {
      if (context.type === 'connectioncreate') {
        const connection = context.data as Schemes['Connection'] & {
          type?: string;
          remove?: () => void;
        };
        const source = this.editor.getNode(connection.source);
        connection.type =
          source?.dto.kind === 'Setup' && connection.sourceOutput === 'funds'
            ? this.editor.getNode(connection.target)?.inputs[connection.targetInput]?.socket.name
            : source?.outputs[connection.sourceOutput]?.socket.name;
        connection.choose = () => this.chooseEdge(connection.id);
        connection.remove = () => {
          void this.editor.removeConnection(connection.id);
        };
        connection.dragStart = (segment) => this.startEdgeDrag(connection.id, segment);
        connection.dragMove = (point) => this.moveEdgeSegment(point);
        connection.dragEnd = (cancel) => this.endEdgeDrag(cancel);
      }
      if (
        !this.syncing &&
        (context.type === 'connectioncreate' || context.type === 'connectionremove')
      ) {
        // Replacement removes the old wire and creates a new one in the same interaction.
        if (!this.connectionHistoryPending) this.remember();
        this.connectionHistoryPending = true;
        clearTimeout(this.connectionHistoryTimer);
        this.connectionHistoryTimer = setTimeout(() => (this.connectionHistoryPending = false), 0);
      }
      if (
        !this.syncing &&
        (context.type === 'connectioncreated' || context.type === 'connectionremoved')
      ) {
        this.graphValue = {
          ...this.graphValue,
          links: this.editor.getConnections().map((c) => ({
            from: c.source,
            to: c.target,
            input: c.targetInput,
            output: c.sourceOutput,
          })),
        };
        if (context.type === 'connectionremoved') {
          const removed = context.data;
          const key = edgeId(
            removed.source,
            removed.sourceOutput,
            removed.target,
            removed.targetInput,
          );
          if (this.layoutValue.edgeRoutes?.[key]) {
            delete this.layoutValue.edgeRoutes[key];
            this.layoutChange.emit(cloneLayout(this.layoutValue));
          }
          this.drawnPoints.delete(removed.id);
          this.routeCache.delete(key);
          this.arrangedRoutes.delete(key);
          if (this.selectedEdge() === removed.id) this.chooseEdge(null);
        }
        this.graphChange.emit(cloneGraph(this.graphValue));
      }
      return context;
    });
    area.addPipe((context) => {
      if (context.type === 'zoom' && (context.data.zoom < 0.05 || context.data.zoom > 1.8)) return;
      if (context.type === 'zoomed') {
        this.zoom.set(context.data.zoom);
        this.refreshConnections();
      }
      if (context.type === 'nodetranslated' || context.type === 'noderesized')
        this.refreshConnections();
      if (context.type === 'nodepicked') {
        this.dragRoutes = new Map(
          [...this.routeCache].map(([id, route]) => [id, structuredClone(route.points)]),
        );
        this.chooseEdge(null);
        this.select.emit(context.data.id);
      }
      if (context.type === 'nodedragged' && !this.syncing) {
        this.remember();
        this.persistDraggedRoutes();
        this.dragRoutes = undefined;
        this.refreshConnections();
        this.persistPositions();
      }
      if (
        context.type === 'pointerdown' &&
        context.data.event.target === this.canvas().nativeElement
      ) {
        this.chooseEdge(null);
        this.select.emit(null);
      }
      return context;
    });
    AreaExtensions.simpleNodesOrder(area);
    this.observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const id = (entry.target as HTMLElement).dataset['reteNode'];
        if (id) {
          const node = this.editor.getNode(id);
          if (node) {
            node.width = entry.contentRect.width;
            node.height = entry.contentRect.height;
          }
          void area.update('node', id);
          this.refreshConnections();
        }
      }
    });
    this.ready = true;
    this.enqueue(async () => {
      await this.sync(
        this.graph(),
        this.layout(),
        this.vocabulary(),
        this.selectedNodeId(),
        this.problems(),
        this.signals(),
        this.instruments(),
        this.setup(),
      );
      await this.measureNodes();
      const missing = this.editor.getNodes().some((node) => !this.layoutValue.positions[node.id]);
      if (missing || this.hasOverlaps()) await this.arrangeAll();
      await this.fit();
    });
  }
  private enqueue(action: () => Promise<void>) {
    this.pending = this.pending
      .then(async () => {
        if (!this.destroyed) await action();
      })
      .catch((error) => {
        console.error('Strategy graph', error);
        this.error.set(translate('builder.graph.editor.updateFailed'));
      });
  }
  private async sync(
    graph: GraphDto,
    layout: LayoutDto,
    vocabulary: VocabularyDto,
    selected: string | null,
    problems: DraftProblemDto[],
    signals: OwnSignalDto[],
    instruments: InstrumentChoiceDto[],
    setup: DraftSetupDto,
  ) {
    this.syncing = true;
    try {
      const normalized = withPortfolioFlow(withSetup(graph));
      if (normalized !== graph) {
        graph = normalized;
        this.graphChange.emit(cloneGraph(graph));
      }
      this.graphValue = cloneGraph(graph);
      this.layoutValue = cloneLayout(layout);
      for (const connection of this.editor.getConnections()) {
        if (
          !graph.links.some(
            (l) =>
              l.from === connection.source &&
              l.to === connection.target &&
              l.input === connection.targetInput &&
              (l.output ?? 'output') === connection.sourceOutput,
          )
        )
          await this.editor.removeConnection(connection.id);
      }
      for (const node of this.editor.getNodes())
        if (!graph.nodes.some((n) => n.id === node.id)) {
          const element = this.area!.nodeViews.get(node.id)?.element;
          if (element) this.observer?.unobserve(element);
          await this.editor.removeNode(node.id);
        }
      const branch = this.branchNodes(graph, selected);
      for (const dto of graph.nodes) {
        let node = this.editor.getNode(dto.id);
        if (!node) {
          node = new StrategyNode(
            dto,
            vocabulary,
            graph,
            signals,
            instruments,
            (value) => this.updateNode(value),
            () => this.select.emit(dto.id),
            () => this.duplicate(dto.id),
            () => this.remove(dto.id),
            (key) => this.keyboardConnect(dto.id, key),
            (key) => this.keyboardStart(dto.id, key),
            setup,
            (value) => {
              this.remember();
              this.setupChange.emit(value);
            },
            () => this.instrumentsChanged.emit(),
          );
          await this.editor.addNode(node);
          const element = this.area!.nodeViews.get(node.id)!.element;
          element.dataset['reteNode'] = node.id;
          this.observer?.observe(element);
        }
        node.setup = setup;
        node.overlayContainer = this.overlayContainer();
        node.dto = dto;
        node.vocabulary = vocabulary;
        node.graph = graph;
        node.updateSetupOutputs();
        node.signals = signals;
        node.instruments = instruments;
        node.selected = selected === dto.id;
        node.branchDimmed = !!branch && !branch.has(dto.id);
        node.problem = problems.find((p) => p.nodeId === dto.id);
        await this.area!.update('node', dto.id);
        const position = layout.positions[dto.id] ?? { x: 32, y: 32 };
        const current = this.area!.nodeViews.get(dto.id)!.position;
        if (current.x !== position.x || current.y !== position.y)
          await this.area!.translate(dto.id, position);
      }
      for (const link of graph.links) {
        if (
          this.editor
            .getConnections()
            .some(
              (c) =>
                c.source === link.from &&
                c.target === link.to &&
                c.targetInput === link.input &&
                c.sourceOutput === (link.output ?? 'output'),
            )
        )
          continue;
        const from = this.editor.getNode(link.from),
          to = this.editor.getNode(link.to);
        if (from?.outputs[link.output ?? 'output'] && to?.inputs[link.input])
          await this.editor.addConnection(
            new ClassicPreset.Connection<ClassicPreset.Node, ClassicPreset.Node>(
              from,
              link.output ?? 'output',
              to,
              link.input,
            ),
          );
      }
      this.refreshConnections();
    } finally {
      this.syncing = false;
    }
  }
  protected async focusDoubleClickedNode(event: MouseEvent): Promise<void> {
    const target = event.target;
    if (!(target instanceof Element) || !this.area) return;
    if (target.closest('button, input, select, textarea, [role="combobox"], .parameters, .fields'))
      return;
    const id = target.closest<HTMLElement>('[data-node-id]')?.dataset['nodeId'];
    const node = id ? this.editor.getNode(id) : undefined;
    if (node) await AreaExtensions.zoomAt(this.area, [node], { scale: 0.85 });
  }

  async focusSetup(): Promise<void> {
    this.roleNavigation = null;
    const node = this.editor.getNode(setupNodeId);
    if (node && this.area) await AreaExtensions.zoomAt(this.area, [node], { scale: 0.85 });
  }
  private roleNavigation: { block: string; nodeId: string } | null = null;

  protected async focusRole(block: string) {
    if (!this.area) return;
    const result = this.editor
      .getNodes()
      .find((node) => node.dto.kind.endsWith('Result') && node.dto.block === block);
    if (!result) return;
    const ids = roleDependenciesOf(this.graphValue, result.id);
    const nodes = this.editor
      .getNodes()
      .filter(
        (node) =>
          ids.has(node.id) &&
          !['Setup', 'Cash', 'Portfolio'].includes(node.dto.kind) &&
          (!node.dto.kind.endsWith('Result') || node.id === result.id),
      )
      .sort((left, right) => {
        const a = this.layoutValue.positions[left.id] ?? { x: 0, y: 0 };
        const b = this.layoutValue.positions[right.id] ?? { x: 0, y: 0 };
        return a.x - b.x || a.y - b.y || left.id.localeCompare(right.id);
      });
    const previous =
      this.roleNavigation?.block === block
        ? nodes.findIndex((node) => node.id === this.roleNavigation!.nodeId)
        : -1;
    const node = nodes[(previous + 1) % nodes.length];
    if (!node) return;
    this.roleNavigation = { block, nodeId: node.id };
    this.chooseEdge(null);
    this.select.emit(node.id);
    await AreaExtensions.zoomAt(this.area, [node], { scale: 0.85 });
  }
  private connectionProblem(
    from: string,
    to: string,
    key: string,
    output = 'output',
  ): string | null {
    const source = this.editor.getNode(from),
      target = this.editor.getNode(to);
    if (!source || !target) return translate('builder.graph.editor.chooseSockets');
    if (
      source.outputs[output]?.socket.name !== target.inputs[key]?.socket.name &&
      !(
        source.dto.kind === 'Setup' &&
        output === 'funds' &&
        target.inputs[key]?.socket.name === 'Candidates'
      )
    )
      return translate('builder.graph.editor.wrongType');
    const pending = [to],
      seen = new Set<string>();
    while (pending.length && !(source.dto.kind === 'Cash' && output === 'series')) {
      const id = pending.pop()!;
      if (id === from) return translate('builder.graph.editor.cycle');
      if (seen.has(id)) continue;
      seen.add(id);
      pending.push(
        ...this.graphValue.links
          .filter((l) => l.from === id && !(l.from === 'strategy-cash' && l.output === 'series'))
          .map((l) => l.to),
      );
    }
    const candidate = {
      ...this.graphValue,
      links: [
        ...this.graphValue.links.filter(
          (link) =>
            !(link.to === to && link.input === key && !target.inputs[key]?.multipleConnections),
        ),
        { from, to, input: key, output },
      ],
    };
    if (candidatePath(candidate).filter((node) => node.kind === 'FundSelection').length > 1)
      return translate('builder.graph.editor.oneSelection');
    if (
      target.dto.kind === 'Cash' &&
      key === 'remainder' &&
      !(source.dto.kind === 'SelectionResult' && output === 'cash')
    )
      return translate('builder.graph.editor.cashGetsRemainder');
    if (
      target.dto.kind === 'SelectionResult' &&
      key === 'budget' &&
      source.dto.kind !== 'MarketResult'
    )
      return translate('builder.graph.editor.budgetFromMarket');
    if (
      target.dto.kind === 'Portfolio' &&
      key === 'cash' &&
      !(source.dto.kind === 'Cash' && output === 'output')
    )
      return translate('builder.graph.editor.portfolioGetsCash');
    const market = candidate.nodes.find((node) => node.kind === 'MarketResult');
    if (market) {
      const ids = dependenciesOf(candidate, market.id);
      if (
        candidate.nodes.some(
          (node) =>
            ids.has(node.id) &&
            fundContext({
              ...node,
              parameters: {
                ...node.parameters,
                of: resolvedSeries(node, candidate, this.setup()) ?? null,
              },
            }),
        )
      )
        return translate('builder.graph.editor.marketNotFund');
    }
    return null;
  }
  private refreshPorts() {
    for (const node of this.editor.getNodes()) {
      node.connecting = !!this.keyboardSource || (!!this.picked && this.picked.side === 'output');
      node.compatible = Object.keys(node.inputs).filter(
        (key) =>
          !this.connectionProblem(
            this.keyboardSource ?? this.picked?.nodeId ?? '',
            node.id,
            key,
            this.keyboardSource ? this.keyboardOutput : (this.picked?.key ?? 'output'),
          ),
      );
      void this.area?.update('node', node.id);
    }
  }
  protected cancelWire() {
    this.keyboardSource = null;
    this.connection.drop();
    this.connecting.set(false);
    this.refreshPorts();
    this.error.set(null);
  }
  private keyboardStart(id: string, output = 'output') {
    this.cancelWire();
    this.keyboardSource = id;
    this.keyboardOutput = output;
    this.connecting.set(true);
    this.refreshPorts();
  }
  private keyboardConnect(id: string, key: string) {
    const from = this.keyboardSource;
    if (!from) return;
    const reason = this.connectionProblem(from, id, key, this.keyboardOutput);
    this.error.set(reason);
    if (reason) return;
    this.enqueue(async () => {
      this.remember();
      this.syncing = true;
      try {
        const target = this.editor.getNode(id)!;
        for (const link of this.editor.getConnections()) {
          if (
            link.target === id &&
            link.targetInput === key &&
            !target.inputs[key]?.multipleConnections
          )
            await this.editor.removeConnection(link.id);
        }
        if (
          !this.editor
            .getConnections()
            .some((c) => c.source === from && c.target === id && c.targetInput === key)
        )
          await this.editor.addConnection(
            new ClassicPreset.Connection<ClassicPreset.Node, ClassicPreset.Node>(
              this.editor.getNode(from)!,
              this.keyboardOutput,
              target,
              key,
            ),
          );
        this.graphValue.links = this.editor.getConnections().map((c) => ({
          from: c.source,
          to: c.target,
          input: c.targetInput,
          output: c.sourceOutput,
        }));
        this.emit();
        this.cancelWire();
      } finally {
        this.syncing = false;
      }
    });
  }
  async toggleFullscreen(): Promise<void> {
    try {
      if (document.fullscreenElement === this.workspace().nativeElement) {
        await document.exitFullscreen();
      } else {
        await this.workspace().nativeElement.requestFullscreen();
      }
    } catch {
      this.error.set(translate('builder.graph.editor.fullscreenFailed'));
    }
  }
  @HostListener('document:fullscreenchange')
  protected fullscreenChanged(): void {
    const fullscreen = document.fullscreenElement === this.workspace().nativeElement;
    this.isFullscreen.set(fullscreen);
    const container = fullscreen ? this.workspace().nativeElement : 'body';
    this.overlayContainer.set(container);
    for (const node of this.editor.getNodes()) {
      node.overlayContainer = container;
      void this.area?.update('node', node.id);
    }
  }
  async fit() {
    if (this.area && this.editor.getNodes().length)
      await AreaExtensions.zoomAt(this.area, this.editor.getNodes(), { scale: 0.85 });
  }
  async setZoom(direction: number) {
    if (!this.area) return;
    const canvas = this.canvas().nativeElement;
    const { x, y, k } = this.area.area.transform;
    const next = Math.max(0.05, Math.min(1.8, k * (direction > 0 ? 1.2 : 1 / 1.2)));
    if (next === k) return;
    const factor = next / k - 1;
    // Rete expects translation deltas here, so the graph point under the
    // viewport centre must stay in place as the scale changes.
    await this.area.area.zoom(
      next,
      (x - canvas.clientWidth / 2) * factor,
      (y - canvas.clientHeight / 2) * factor,
    );
  }
  protected addNode(request: NewPaletteNode, position?: NodePosition) {
    this.paletteOpen.set(false);
    this.remember();
    const dto: NodeDto = {
      id: crypto.randomUUID(),
      block: request.block,
      kind: request.kind,
      parameters: structuredClone(request.parameters),
    };
    const el = this.canvas().nativeElement,
      t = this.area!.area.transform;
    const at = snapPosition(
      position ?? { x: (el.clientWidth / 2 - t.x) / t.k - 120, y: (100 - t.y) / t.k },
    );
    this.layoutValue.positions[dto.id] = at;
    this.graphValue.nodes.push(dto);
    this.emit();
    this.select.emit(dto.id);
  }
  protected drop(event: DragEvent) {
    event.preventDefault();
    const raw = event.dataTransfer?.getData('application/strategy-node');
    if (!raw) return;
    try {
      const request = JSON.parse(raw) as NewPaletteNode;
      if (!this.vocabulary().nodeKinds.some((k) => k.kind === request.kind)) return;
      const bounds = this.canvas().nativeElement.getBoundingClientRect(),
        t = this.area!.area.transform;
      this.addNode(request, {
        x: (event.clientX - bounds.left - t.x) / t.k - 120,
        y: (event.clientY - bounds.top - t.y) / t.k - 24,
      });
    } catch {
      this.error.set(translate('builder.graph.editor.addFailed'));
    }
  }
  private updateNode(node: NodeDto) {
    this.remember();
    this.graphValue.nodes = this.graphValue.nodes.map((n) => (n.id === node.id ? node : n));
    this.emit();
  }
  private duplicate(id: string) {
    const node = this.graphValue.nodes.find((n) => n.id === id);
    if (
      node &&
      !node.kind.endsWith('Result') &&
      node.kind !== 'Setup' &&
      !['Cash', 'Portfolio'].includes(node.kind)
    ) {
      const p = this.layoutValue.positions[id];
      this.addNode({ ...node, block: 'Shared' }, { x: (p?.x ?? 0) + 48, y: (p?.y ?? 0) + 48 });
    }
  }
  private remove(id: string) {
    const node = this.graphValue.nodes.find((n) => n.id === id);
    if (
      !node ||
      node.kind.endsWith('Result') ||
      node.kind === 'Setup' ||
      ['Cash', 'Portfolio'].includes(node.kind)
    )
      return;
    this.remember();
    this.graphValue = {
      nodes: this.graphValue.nodes.filter((n) => n.id !== id),
      links: this.graphValue.links.filter((l) => l.from !== id && l.to !== id),
    };
    delete this.layoutValue.positions[id];
    this.emit();
    this.select.emit(null);
  }
  private persistDraggedRoutes() {
    if (!this.dragRoutes) return;
    for (const connection of this.editor.getConnections()) {
      const key = edgeId(
        connection.source,
        connection.sourceOutput,
        connection.target,
        connection.targetInput,
      );
      if (!this.layoutValue.edgeRoutes?.[key]) continue;
      const points = this.dragRoutes.get(key);
      if (!points) continue;
      const delta = (id: string) => {
        const before = this.layoutValue.positions[id],
          after = this.area!.nodeViews.get(id)?.position;
        return before && after ? { x: after.x - before.x, y: after.y - before.y } : { x: 0, y: 0 };
      };
      const from = delta(connection.source),
        to = delta(connection.target);
      if (!from.x && !from.y && !to.x && !to.y) continue;
      const first = points[0],
        last = points.at(-1)!;
      this.layoutValue.edgeRoutes[key] = editablePoints(
        retargetRoute(
          points,
          { x: first.x + from.x, y: first.y + from.y },
          { x: last.x + to.x, y: last.y + to.y },
        ),
      ).slice(1, -1);
    }
  }
  private persistPositions() {
    for (const [id, view] of this.area!.nodeViews)
      this.layoutValue.positions[id] = { ...view.position };
    this.layoutChange.emit(cloneLayout(this.layoutValue));
  }
  private emit() {
    this.graphChange.emit(cloneGraph(this.graphValue));
    this.layoutChange.emit(cloneLayout(this.layoutValue));
  }
  private snapshot(): Snapshot {
    return {
      graph: cloneGraph(this.graphValue),
      layout: cloneLayout(this.layoutValue),
      setup: structuredClone(this.setup()),
    };
  }
  private remember() {
    this.undoStack.push(this.snapshot());
    if (this.undoStack.length > 100) this.undoStack.shift();
    this.redoStack = [];
    this.historyState();
  }
  private historyState() {
    this.canUndo.set(!!this.undoStack.length);
    this.canRedo.set(!!this.redoStack.length);
  }
  undo() {
    const s = this.undoStack.pop();
    if (s) {
      this.redoStack.push(this.snapshot());
      this.restore(s);
    }
  }
  redo() {
    const s = this.redoStack.pop();
    if (s) {
      this.undoStack.push(this.snapshot());
      this.restore(s);
    }
  }
  private restore(s: Snapshot) {
    this.routeCache.clear();
    this.arrangedRoutes.clear();
    this.dragRoutes = undefined;
    this.chooseEdge(null);
    this.cancelWire();
    this.setupChange.emit(structuredClone(s.setup));
    this.graphValue = s.graph;
    this.layoutValue = s.layout;
    this.emit();
    this.historyState();
  }
  async arrangeAll() {
    this.remember();
    await this.measureNodes();
    const nodes = this.editor.getNodes();
    const measured = nodes.map((node) => {
      const element = this.area!.nodeViews.get(node.id)!.element;
      const article = element.querySelector('article')!;
      const rect = article.getBoundingClientRect();
      const scale = this.area!.area.transform.k;
      const ports = [...element.querySelectorAll<HTMLElement>('[data-socket-key]')].map((port) => {
        const bounds = port.getBoundingClientRect();
        const side = port.dataset['socketKey']!.includes(':input:') ? 'input' : 'output';
        const key = port.dataset['socketKey']!.slice(`${node.id}:${side}`.length + 1) || 'output';
        return {
          id: portId(node.id, side, key),
          side: side === 'input' ? ('WEST' as const) : ('EAST' as const),
          y: (bounds.top + bounds.height / 2 - rect.top) / scale,
        };
      });
      return { id: node.id, width: node.width, height: node.height, ports };
    });
    try {
      const arranged = await layoutGraph(this.graphValue, measured);
      if (this.destroyed) return;
      for (const node of nodes) await this.area!.translate(node.id, arranged.positions[node.id]);
      this.arrangedRoutes = arranged.routes;
      this.routeCache.clear();
      this.refreshConnections();
    } catch (error) {
      console.error('Strategy layout', error);
      this.error.set(translate('builder.graph.editor.arrangeFailed'));
      return;
    }
    this.persistPositions();
    await this.fit();
  }
  private nodeBoxes(): NodeBox[] {
    return this.editor.getNodes().map((node) => ({
      id: node.id,
      ...this.area!.nodeViews.get(node.id)!.position,
      width: node.width,
      height: node.height,
    }));
  }
  private boxSignature(boxes: NodeBox[]) {
    return JSON.stringify(boxes);
  }
  private connectionPath(connection: Schemes['Connection'], start: Point, end: Point): string {
    const boxes = this.nodeBoxes(),
      signature = this.boxSignature(boxes);
    const id = edgeId(
      connection.source,
      connection.sourceOutput,
      connection.target,
      connection.targetInput,
    );
    const manual = this.layoutValue.edgeRoutes?.[id];
    const manualKey = JSON.stringify(manual ?? null);
    const geometry = signature + (this.dragRoutes ? ':drag' : ':idle');
    let cached = this.routeCache.get(id);
    const previous = cached?.manual === manualKey ? cached : undefined;
    const origin = this.dragRoutes?.get(id) ?? previous?.points;
    const sameEndpoints =
      previous &&
      Math.hypot(previous.points[0].x - start.x, previous.points[0].y - start.y) < 0.01 &&
      Math.hypot(previous.points.at(-1)!.x - end.x, previous.points.at(-1)!.y - end.y) < 0.01;
    if (!previous || !sameEndpoints || previous.geometry !== geometry) {
      let points: Point[];
      if (origin && previous) {
        points = sameEndpoints
          ? previous.points
          : editablePoints(retargetRoute(origin, start, end));
        // Incremental routing follows the corridor invalidation principle in
        // https://tisoap.github.io/react-flow-smart-edge/docs/guides/performance/
        // Preserve a valid route, even when a newly shorter route exists. During
        // a drag only retarget endpoints; obstacle search happens once on drop.
        if (
          !manual?.length &&
          !this.dragRoutes &&
          !routeIsClear(points, boxes, connection.source, connection.target)
        ) {
          points = editablePoints(
            routePoints(start, end, boxes, connection.source, connection.target),
          );
        }
      } else if (manual?.length) {
        points = editablePoints(anchoredPoints([start, ...manual, end]));
      } else {
        const arranged = this.arrangedRoutes.get(id);
        points = editablePoints(
          anchoredPoints(
            arranged
              ? [start, ...arranged, end]
              : routePoints(start, end, boxes, connection.source, connection.target),
          ),
        );
        this.arrangedRoutes.delete(id);
      }
      cached = { path: roundedPath(points), points, manual: manualKey, geometry };
      this.routeCache.set(id, cached);
    }
    const route = cached!;
    this.drawnPoints.set(connection.id, route.points);
    connection.selected = this.selectedEdge() === connection.id;
    const branch = this.branchNodes(this.graphValue, this.selectedNodeId());
    connection.branchDimmed =
      !!branch && (!branch.has(connection.source) || !branch.has(connection.target));
    connection.handleRadius = 6 / this.area!.area.transform.k;
    connection.handles = connection.selected ? edgeHandles(route.points) : [];
    return route.path;
  }
  private chooseEdge(id: string | null) {
    this.selectedEdge.set(id);
    if (id) this.select.emit(null);
    for (const connection of this.editor.getConnections())
      connection.selected = connection.id === id;
    this.refreshConnections();
  }
  protected async removeSelectedEdge() {
    const id = this.selectedEdge();
    if (id) await this.editor.removeConnection(id);
  }
  protected resetEdgeRoute() {
    const connection = this.editor.getConnection(this.selectedEdge() ?? '');
    if (!connection) return;
    const key = edgeId(
      connection.source,
      connection.sourceOutput,
      connection.target,
      connection.targetInput,
    );
    if (!this.layoutValue.edgeRoutes?.[key]) return;
    this.remember();
    delete this.layoutValue.edgeRoutes[key];
    this.routeCache.delete(key);
    this.layoutChange.emit(cloneLayout(this.layoutValue));
    this.refreshConnections();
  }
  private startEdgeDrag(id: string, segment: number) {
    const connection = this.editor.getConnection(id),
      points = this.drawnPoints.get(id);
    if (!connection || !points || segment < 1 || segment >= points.length - 2) return;
    this.chooseEdge(id);
    const key = edgeId(
      connection.source,
      connection.sourceOutput,
      connection.target,
      connection.targetInput,
    );
    this.edgeDrag = {
      id,
      key,
      segment,
      axis: points[segment].x === points[segment + 1].x ? 'x' : 'y',
      points: structuredClone(points),
      layout: cloneLayout(this.layoutValue),
    };
    this.remember();
  }
  private moveEdgeSegment(screen: Point) {
    const drag = this.edgeDrag;
    if (!drag) return;
    const bounds = this.canvas().nativeElement.getBoundingClientRect(),
      transform = this.area!.area.transform;
    const position = {
      x: (screen.x - bounds.left - transform.x) / transform.k,
      y: (screen.y - bounds.top - transform.y) / transform.k,
    };
    const points = structuredClone(drag.points);
    const value = Math.round(position[drag.axis] / 8) * 8;
    points[drag.segment][drag.axis] = value;
    points[drag.segment + 1][drag.axis] = value;
    this.layoutValue.edgeRoutes = {
      ...this.layoutValue.edgeRoutes,
      [drag.key]: points.slice(1, -1),
    };
    this.refreshConnections();
  }
  private endEdgeDrag(cancel: boolean) {
    const drag = this.edgeDrag;
    if (!drag) return;
    this.edgeDrag = undefined;
    if (cancel) {
      this.layoutValue = drag.layout;
      this.undoStack.pop();
      this.historyState();
    } else this.layoutChange.emit(cloneLayout(this.layoutValue));
    this.refreshConnections();
  }
  private refreshConnections() {
    if (this.routeFrame !== undefined) return;
    this.routeFrame = requestAnimationFrame(() => {
      this.routeFrame = undefined;
      if (!this.destroyed)
        for (const connection of this.editor.getConnections())
          void this.area?.update('connection', connection.id);
    });
  }
  private async measureNodes(): Promise<void> {
    let previous = '';
    for (let frame = 0; frame < 20; frame++) {
      await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
      if (this.destroyed) return;
      const sizes: string[] = [];
      for (const node of this.editor.getNodes()) {
        const element = this.area?.nodeViews.get(node.id)?.element;
        const article = element?.querySelector<HTMLElement>('article');
        if (!article) continue;
        node.width = article.offsetWidth;
        node.height = article.offsetHeight;
        sizes.push(`${node.id}:${node.width}:${node.height}`);
      }
      const current = sizes.join('|');
      if (sizes.length === this.editor.getNodes().length && current === previous) return;
      previous = current;
    }
  }
  private hasOverlaps(): boolean {
    const nodes = this.editor.getNodes();
    return nodes.some((node, index) =>
      nodes.slice(index + 1).some((other) => {
        const a = this.area!.nodeViews.get(node.id)!.position,
          b = this.area!.nodeViews.get(other.id)!.position;
        return (
          a.x < b.x + other.width + 16 &&
          a.x + node.width + 16 > b.x &&
          a.y < b.y + other.height + 16 &&
          a.y + node.height + 16 > b.y
        );
      }),
    );
  }
  @HostListener('keydown', ['$event']) protected key(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      this.cancelWire();
      this.endEdgeDrag(true);
      this.chooseEdge(null);
    }
    if (
      (event.target as HTMLElement).closest(
        'input,textarea,[role="combobox"],[role="spinbutton"],[contenteditable="true"]',
      )
    )
      return;
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z') {
      event.preventDefault();
      event.shiftKey ? this.redo() : this.undo();
    }
    if ((event.key === 'Delete' || event.key === 'Backspace') && this.selectedEdge()) {
      event.preventDefault();
      void this.removeSelectedEdge();
    } else if (event.key === 'Delete' && this.selectedNodeId()) this.remove(this.selectedNodeId()!);
  }
  ngOnDestroy() {
    this.destroyed = true;
    clearTimeout(this.connectionHistoryTimer);
    this.observer?.disconnect();
    if (this.routeFrame !== undefined) cancelAnimationFrame(this.routeFrame);
    this.connection.drop();
    this.area?.destroy();
  }
}
