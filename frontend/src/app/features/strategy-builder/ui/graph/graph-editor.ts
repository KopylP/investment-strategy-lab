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
import { AreaPlugin, AreaExtensions } from 'rete-area-plugin';
import { getDOMSocketPosition } from 'rete-render-utils';
import { AngularPlugin, AngularArea2D, Presets } from 'rete-angular-plugin/21';
import { ConnectionPlugin, ClassicFlow, SocketData } from 'rete-connection-plugin';
import {
  BlockWordsDto,
  DraftProblemDto,
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
import { blocks } from '../../models/draft-vm';
import { dependenciesOf, resultsUsing, fundContext } from '../../data-access/shared-graph';

interface Snapshot {
  graph: GraphDto;
  layout: LayoutDto;
}
type Extra = AngularArea2D<Schemes>;
@Component({
  selector: 'app-graph-editor',
  imports: [Palette],
  templateUrl: './graph-editor.html',
  styleUrl: './graph-editor.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraphEditor implements AfterViewInit, OnDestroy {
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
  protected readonly isFullscreen = signal(false);
  protected readonly overlayContainer = signal<HTMLElement | 'body'>('body');
  private readonly canvas = viewChild.required<ElementRef<HTMLElement>>('canvas');
  private readonly injector = inject(Injector);
  protected readonly zoom = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly connecting = signal(false);
  protected readonly paletteOpen = signal(false);
  protected readonly canUndo = signal(false);
  protected readonly canRedo = signal(false);
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
  private lastSelected: string | null = null;
  protected readonly roles = blocks;
  private observer?: ResizeObserver;
  private connectionHistoryPending = false;
  private connectionHistoryTimer?: ReturnType<typeof setTimeout>;
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
      if (this.ready)
        this.enqueue(() =>
          this.sync(graph, layout, vocabulary, selected, problems, signals, instruments),
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
          node: () => ReteNode,
          connection: () => ReteConnection,
          socket: () => ReteSocket,
        },
      }),
    );
    this.editor.use(area);
    area.use(this.connection);
    area.use(render);
    this.connection.addPreset(
      () =>
        new ClassicFlow({
          canMakeConnection: (a, b) => {
            const source = a.side === 'output' ? a : b;
            const target = a.side === 'input' ? a : b;
            const reason = this.connectionProblem(source.nodeId, target.nodeId, target.key);
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
        connection.type = this.editor.getNode(connection.source)?.outputs['output']?.socket.name;
        connection.remove = () => {
          void this.editor.removeConnection(connection.id);
        };
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
          links: this.editor
            .getConnections()
            .map((c) => ({ from: c.source, to: c.target, input: c.targetInput })),
        };
        this.graphChange.emit(cloneGraph(this.graphValue));
      }
      return context;
    });
    area.addPipe((context) => {
      if (context.type === 'zoom' && (context.data.zoom < 0.05 || context.data.zoom > 1.8)) return;
      if (context.type === 'zoomed') this.zoom.set(context.data.zoom);
      if (context.type === 'nodepicked') this.select.emit(context.data.id);
      if (context.type === 'nodedragged' && !this.syncing) {
        this.remember();
        this.persistPositions();
      }
      if (
        context.type === 'pointerdown' &&
        context.data.event.target === this.canvas().nativeElement
      )
        this.select.emit(null);
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
      );
      const positions = Object.values(this.layoutValue.positions);
      const overlaps =
        new Set(positions.map((p) => `${p.x}:${p.y}`)).size < positions.length ||
        this.graph().nodes.some((node) => !this.layout().positions[node.id]);
      if (overlaps) await this.arrangeAll();
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
        this.error.set('Не вдалося оновити схему. Спробуйте ще раз.');
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
  ) {
    this.syncing = true;
    try {
      this.graphValue = cloneGraph(graph);
      this.layoutValue = cloneLayout(layout);
      for (const connection of this.editor.getConnections()) {
        if (
          !graph.links.some(
            (l) =>
              l.from === connection.source &&
              l.to === connection.target &&
              l.input === connection.targetInput,
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
            () => this.keyboardStart(dto.id),
          );
          await this.editor.addNode(node);
          const element = this.area!.nodeViews.get(node.id)!.element;
          element.dataset['reteNode'] = node.id;
          this.observer?.observe(element);
        }
        node.overlayContainer = this.overlayContainer();
        node.dto = dto;
        node.vocabulary = vocabulary;
        node.graph = graph;
        node.signals = signals;
        node.instruments = instruments;
        node.selected = selected === dto.id;
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
              (c) => c.source === link.from && c.target === link.to && c.targetInput === link.input,
            )
        )
          continue;
        const from = this.editor.getNode(link.from),
          to = this.editor.getNode(link.to);
        if (from?.outputs['output'] && to?.inputs[link.input])
          await this.editor.addConnection(
            new ClassicPreset.Connection<ClassicPreset.Node, ClassicPreset.Node>(
              from,
              'output',
              to,
              link.input,
            ),
          );
      }
      if (selected && selected !== this.lastSelected && this.zoom() < 0.55) {
        const node = this.editor.getNode(selected);
        if (node) await AreaExtensions.zoomAt(this.area!, [node], { scale: 0.75 });
      }
      this.lastSelected = selected;
    } finally {
      this.syncing = false;
    }
  }
  protected async focusRole(block: string) {
    const result = this.editor
      .getNodes()
      .find((node) => node.dto.kind.endsWith('Result') && node.dto.block === block);
    if (!result) return;
    const ids = dependenciesOf(this.graphValue, result.id);
    const nodes = this.editor.getNodes().filter((node) => ids.has(node.id));
    await AreaExtensions.zoomAt(this.area!, nodes, { scale: 0.8 });
  }
  private connectionProblem(from: string, to: string, key: string): string | null {
    const source = this.editor.getNode(from),
      target = this.editor.getNode(to);
    if (!source || !target) return 'Оберіть вихід та вхід вузла.';
    if (source.outputs['output']?.socket.name !== target.inputs[key]?.socket.name)
      return 'Цей вхід очікує інший тип даних. Оберіть підсвічений вхід.';
    const pending = [to],
      seen = new Set<string>();
    while (pending.length) {
      const id = pending.pop()!;
      if (id === from) return 'Цей зв’язок створює цикл. Оберіть інший вхід.';
      if (seen.has(id)) continue;
      seen.add(id);
      pending.push(...this.graphValue.links.filter((l) => l.from === id).map((l) => l.to));
    }
    const market = resultsUsing(this.graphValue, to).some((node) => node.kind === 'MarketResult');
    if (market) {
      const sources = dependenciesOf(this.graphValue, from);
      if (this.graphValue.nodes.some((node) => sources.has(node.id) && fundContext(node)))
        return 'Ринковий фільтр оцінює ринок загалом. Оберіть бенчмарк або конкретний інструмент замість «цього фонду».';
    }
    return null;
  }
  private refreshPorts() {
    for (const node of this.editor.getNodes()) {
      node.connecting = !!this.keyboardSource || (!!this.picked && this.picked.side === 'output');
      node.compatible = Object.keys(node.inputs).filter(
        (key) =>
          !this.connectionProblem(this.keyboardSource ?? this.picked?.nodeId ?? '', node.id, key),
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
  private keyboardStart(id: string) {
    this.cancelWire();
    this.keyboardSource = id;
    this.connecting.set(true);
    this.refreshPorts();
  }
  private keyboardConnect(id: string, key: string) {
    const from = this.keyboardSource;
    if (!from) return;
    const reason = this.connectionProblem(from, id, key);
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
              'output',
              target,
              key,
            ),
          );
        this.graphValue.links = this.editor
          .getConnections()
          .map((c) => ({ from: c.source, to: c.target, input: c.targetInput }));
        this.emit();
        this.cancelWire();
      } finally {
        this.syncing = false;
      }
    });
  }
  protected async toggleFullscreen(): Promise<void> {
    try {
      if (document.fullscreenElement === this.workspace().nativeElement) {
        await document.exitFullscreen();
      } else {
        await this.workspace().nativeElement.requestFullscreen();
      }
    } catch {
      this.error.set('Не вдалося відкрити повноекранний режим. Спробуйте ще раз.');
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
  protected async fit() {
    if (this.area && this.editor.getNodes().length)
      await AreaExtensions.zoomAt(this.area, this.editor.getNodes(), { scale: 0.85 });
  }
  protected async setZoom(delta: number) {
    const el = this.canvas().nativeElement;
    await this.area?.area.zoom(
      Math.max(0.05, Math.min(1.8, this.zoom() + delta)),
      el.clientWidth / 2,
      el.clientHeight / 2,
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
      this.error.set('Не вдалося додати вузол. Оберіть його зі списку.');
    }
  }
  private updateNode(node: NodeDto) {
    this.remember();
    this.graphValue.nodes = this.graphValue.nodes.map((n) => (n.id === node.id ? node : n));
    this.emit();
  }
  private duplicate(id: string) {
    const node = this.graphValue.nodes.find((n) => n.id === id);
    if (node && !node.kind.endsWith('Result')) {
      const p = this.layoutValue.positions[id];
      this.addNode({ ...node, block: 'Shared' }, { x: (p?.x ?? 0) + 48, y: (p?.y ?? 0) + 48 });
    }
  }
  private remove(id: string) {
    const node = this.graphValue.nodes.find((n) => n.id === id);
    if (!node || node.kind.endsWith('Result')) return;
    this.remember();
    this.graphValue = {
      nodes: this.graphValue.nodes.filter((n) => n.id !== id),
      links: this.graphValue.links.filter((l) => l.from !== id && l.to !== id),
    };
    delete this.layoutValue.positions[id];
    this.emit();
    this.select.emit(null);
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
    return { graph: cloneGraph(this.graphValue), layout: cloneLayout(this.layoutValue) };
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
  protected undo() {
    const s = this.undoStack.pop();
    if (s) {
      this.redoStack.push(this.snapshot());
      this.restore(s);
    }
  }
  protected redo() {
    const s = this.redoStack.pop();
    if (s) {
      this.undoStack.push(this.snapshot());
      this.restore(s);
    }
  }
  private restore(s: Snapshot) {
    this.cancelWire();
    this.graphValue = s.graph;
    this.layoutValue = s.layout;
    this.emit();
    this.historyState();
  }
  protected async arrangeAll() {
    this.remember();
    const nodes = this.editor.getNodes();
    const levels = new Map<string, number>();
    const level = (id: string, seen = new Set<string>()): number => {
      if (levels.has(id)) return levels.get(id)!;
      if (seen.has(id)) return 0;
      seen.add(id);
      const parents = this.graphValue.links.filter((link) => link.to === id);
      const value = parents.length
        ? Math.max(...parents.map((link) => level(link.from, new Set(seen)) + 1))
        : 0;
      levels.set(id, value);
      return value;
    };
    const lastColumn =
      Math.max(
        0,
        ...nodes.filter((node) => !node.dto.kind.endsWith('Result')).map((node) => level(node.id)),
      ) + 1;
    const bottoms = new Map<number, number>();
    for (const node of nodes
      .filter((node) => !node.dto.kind.endsWith('Result'))
      .concat(nodes.filter((node) => node.dto.kind.endsWith('Result')))) {
      const col = node.dto.kind.endsWith('Result') ? lastColumn : level(node.id);
      const top = bottoms.get(col) ?? 32;
      await this.area!.translate(node.id, { x: 32 + col * 320, y: top });
      bottoms.set(col, top + node.height + 40);
    }
    this.persistPositions();
    await this.fit();
  }
  @HostListener('keydown', ['$event']) protected key(event: KeyboardEvent) {
    if (event.key === 'Escape') this.cancelWire();
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
    if (event.key === 'Delete' && this.selectedNodeId()) this.remove(this.selectedNodeId()!);
  }
  ngOnDestroy() {
    this.destroyed = true;
    clearTimeout(this.connectionHistoryTimer);
    this.observer?.disconnect();
    this.connection.drop();
    this.area?.destroy();
  }
}
