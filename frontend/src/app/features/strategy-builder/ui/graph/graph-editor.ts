import {
  ChangeDetectionStrategy,
  Component,
  effect,
  HostListener,
  input,
  OnDestroy,
  output,
  signal,
} from '@angular/core';
import { Tooltip } from 'primeng/tooltip';
import {
  BlockWordsDto,
  DraftProblemDto,
  GraphDto,
  LayoutDto,
  LinkDto,
  NodeDto,
  NodePosition,
} from '../../models/draft-dto';
import { blocks } from '../../models/draft-vm';
import { InputSocketDto, SocketType, VocabularyDto } from '../../models/vocabulary-dto';
import {
  arrangedBlock,
  cloneGraph,
  cloneLayout,
  positionsForMissingNodes,
  snapPosition,
} from '../../data-access/graph-mapper';
import { BlockFrame } from './block-frame/block-frame';
import { CompareNode } from './nodes/compare-node';
import { ConditionNode } from './nodes/condition-node';
import { IndicatorNode } from './nodes/indicator-node';
import { LogicNode } from './nodes/logic-node';
import { RangeNode } from './nodes/range-node';
import { ResultNode } from './nodes/result-node';
import { SignalNode } from './nodes/signal-node';
import { SocketShape } from './sockets/socket-shape';
import { CompatibleNodesMenu } from './compatible-nodes-menu';
import { NodeKindDto } from '../../models/vocabulary-dto';

interface EditorSnapshot {
  graph: GraphDto;
  layout: LayoutDto;
}

interface DragState {
  nodeId: string;
  pointerX: number;
  pointerY: number;
  start: NodePosition;
  before: EditorSnapshot;
  bounds: DOMRect;
}

interface ConnectionMenuContext {
  block: string;
  x: number;
  y: number;
  sourceId: string;
  link: LinkDto | null;
}

@Component({
  selector: 'app-graph-editor',
  imports: [
    Tooltip,
    BlockFrame,
    CompareNode,
    ConditionNode,
    IndicatorNode,
    LogicNode,
    RangeNode,
    ResultNode,
    SignalNode,
    SocketShape,
    CompatibleNodesMenu,
  ],
  templateUrl: './graph-editor.html',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraphEditor implements OnDestroy {
  readonly graph = input.required<GraphDto>();
  readonly layout = input.required<LayoutDto>();
  readonly vocabulary = input.required<VocabularyDto>();
  readonly problems = input.required<DraftProblemDto[]>();
  readonly selectedNodeId = input.required<string | null>();
  readonly words = input.required<BlockWordsDto[]>();
  readonly graphChange = output<GraphDto>();
  readonly layoutChange = output<LayoutDto>();
  readonly select = output<string | null>();

  protected readonly blockDefinitions = blocks;
  protected readonly currentGraph = signal<GraphDto>({ nodes: [], links: [] });
  protected readonly currentLayout = signal<LayoutDto>({ positions: {}, collapsed: [] });
  protected readonly wireSource = signal<string | null>(null);
  protected readonly compatibleMenu = signal<ConnectionMenuContext | null>(null);
  private readonly undoStack: EditorSnapshot[] = [];
  private readonly redoStack: EditorSnapshot[] = [];
  private drag: DragState | null = null;
  private readonly moveListener = (event: PointerEvent) => this.moveNode(event);
  private readonly upListener = () => this.stopDragging();

  constructor() {
    effect(() => this.currentGraph.set(cloneGraph(this.graph())));
    effect(() => {
      const arranged = positionsForMissingNodes(this.graph(), this.layout());
      this.currentLayout.set(arranged);

      if (Object.keys(arranged.positions).length !== Object.keys(this.layout().positions).length) {
        queueMicrotask(() => this.layoutChange.emit(cloneLayout(arranged)));
      }
    });
  }

  ngOnDestroy(): void {
    this.removeDragListeners();
  }

  protected nodesIn(block: string): NodeDto[] {
    return this.currentGraph().nodes.filter((node) => node.block === block);
  }

  protected linksIn(block: string): LinkDto[] {
    const nodes = new Set(this.nodesIn(block).map((node) => node.id));
    return this.currentGraph().links.filter((link) => nodes.has(link.from) && nodes.has(link.to));
  }

  protected position(nodeId: string): NodePosition {
    return this.currentLayout().positions[nodeId] ?? { x: 16, y: 16 };
  }

  protected blockHeight(block: string): number {
    const bottom = this.nodesIn(block).reduce(
      (maximum, node) => Math.max(maximum, this.position(node.id).y + 144),
      0,
    );
    return Math.max(280, bottom + 16);
  }

  protected connectionPath(link: LinkDto): string {
    const source = this.position(link.from);
    const target = this.position(link.to);
    const startX = source.x + 224;
    const startY = source.y + 52;
    const endX = target.x;
    const endY = target.y + 52;
    const curve = Math.max(72, Math.abs(endX - startX) / 2);

    return `M ${startX} ${startY} C ${startX + curve} ${startY}, ${endX - curve} ${endY}, ${endX} ${endY}`;
  }

  protected inputsOf(node: NodeDto): InputSocketDto[] {
    return this.vocabulary().nodeKinds.find((kind) => kind.kind === node.kind)?.inputs ?? [];
  }

  protected outputOf(node: NodeDto): SocketType | null {
    return this.vocabulary().nodeKinds.find((kind) => kind.kind === node.kind)?.output ?? null;
  }

  protected isResult(node: NodeDto): boolean {
    return node.kind.endsWith('Result');
  }

  protected wordOf(block: string): string {
    return this.words().find((words) => words.block === block)?.sentence ?? '';
  }

  protected isCollapsed(block: string): boolean {
    return this.currentLayout().collapsed.includes(block as never);
  }

  protected toggleBlock(block: string): void {
    const before = this.snapshot();
    const collapsed = this.isCollapsed(block)
      ? this.currentLayout().collapsed.filter((kind) => kind !== block)
      : [...this.currentLayout().collapsed, block as never];
    const layout = { ...this.currentLayout(), collapsed };

    this.remember(before);
    this.currentLayout.set(layout);
    this.layoutChange.emit(cloneLayout(layout));
  }

  protected arrange(block: string): void {
    const before = this.snapshot();
    const layout = arrangedBlock(this.currentGraph(), this.currentLayout(), block as never);

    this.remember(before);
    this.currentLayout.set(layout);
    this.layoutChange.emit(cloneLayout(layout));
  }

  protected startDragging(event: PointerEvent, node: NodeDto): void {
    if (event.button !== 0 || (event.target as HTMLElement).closest('[data-socket]')) return;

    const canvas = (event.currentTarget as HTMLElement).closest('[data-block-canvas]');
    if (!canvas) return;

    event.preventDefault();
    this.select.emit(node.id);
    this.drag = {
      nodeId: node.id,
      pointerX: event.clientX,
      pointerY: event.clientY,
      start: this.position(node.id),
      before: this.snapshot(),
      bounds: canvas.getBoundingClientRect(),
    };
    window.addEventListener('pointermove', this.moveListener);
    window.addEventListener('pointerup', this.upListener, { once: true });
  }

  protected beginWire(event: PointerEvent, node: NodeDto): void {
    event.stopPropagation();
    event.preventDefault();
    this.wireSource.set(node.id);
    this.compatibleMenu.set(null);
    this.select.emit(node.id);
  }

  protected connect(event: MouseEvent, target: NodeDto, inputSocket: InputSocketDto): void {
    event.stopPropagation();
    const sourceId = this.wireSource();
    if (!sourceId) return;

    const source = this.currentGraph().nodes.find((node) => node.id === sourceId);
    if (!source || source.block !== target.block || this.outputOf(source) !== inputSocket.type)
      return;

    const before = this.snapshot();
    const withoutPrevious = inputSocket.many
      ? this.currentGraph().links
      : this.currentGraph().links.filter(
          (link) => link.to !== target.id || link.input !== inputSocket.name,
        );
    const exists = withoutPrevious.some(
      (link) => link.from === source.id && link.to === target.id && link.input === inputSocket.name,
    );
    if (exists) {
      this.wireSource.set(null);
      return;
    }

    const graph = {
      ...this.currentGraph(),
      links: [...withoutPrevious, { from: source.id, to: target.id, input: inputSocket.name }],
    };

    this.remember(before);
    this.currentGraph.set(graph);
    this.wireSource.set(null);
    this.graphChange.emit(cloneGraph(graph));
  }

  protected socketDimmed(target: NodeDto, inputSocket: InputSocketDto): boolean {
    const sourceId = this.wireSource();
    if (!sourceId) return false;

    const source = this.currentGraph().nodes.find((node) => node.id === sourceId);
    return !source || source.block !== target.block || this.outputOf(source) !== inputSocket.type;
  }

  protected cancelWire(): void {
    this.wireSource.set(null);
    this.compatibleMenu.set(null);
    this.select.emit(null);
  }

  protected canvasClick(event: MouseEvent, block: string): void {
    event.stopPropagation();
    const sourceId = this.wireSource();
    if (!sourceId) {
      this.select.emit(null);
      this.compatibleMenu.set(null);
      return;
    }

    const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.compatibleMenu.set({
      block,
      x: event.clientX - bounds.left,
      y: event.clientY - bounds.top,
      sourceId,
      link: null,
    });
  }

  protected openConnectionMenu(event: MouseEvent, link: LinkDto, block: string): void {
    event.stopPropagation();
    const canvas = (event.currentTarget as SVGPathElement).closest('[data-block-canvas]');
    if (!canvas) return;

    const bounds = canvas.getBoundingClientRect();
    this.compatibleMenu.set({
      block,
      x: event.clientX - bounds.left,
      y: event.clientY - bounds.top,
      sourceId: link.from,
      link,
    });
  }

  protected compatibleKinds(): NodeKindDto[] {
    const menu = this.compatibleMenu();
    if (!menu) return [];

    const source = this.currentGraph().nodes.find((node) => node.id === menu.sourceId);
    const sourceType = source ? this.outputOf(source) : null;
    if (!sourceType) return [];

    if (!menu.link) {
      return this.vocabulary().nodeKinds.filter(
        (kind) =>
          !kind.kind.endsWith('Result') && kind.inputs.some((input) => input.type === sourceType),
      );
    }

    const target = this.currentGraph().nodes.find((node) => node.id === menu.link!.to);
    const targetInput = target
      ? this.inputsOf(target).find((input) => input.name === menu.link!.input)
      : undefined;

    return this.vocabulary().nodeKinds.filter(
      (kind) =>
        !kind.kind.endsWith('Result') &&
        kind.inputs.some((input) => input.type === sourceType) &&
        kind.output === targetInput?.type,
    );
  }

  protected insertCompatible(kind: NodeKindDto): void {
    const menu = this.compatibleMenu();
    if (!menu) return;

    const source = this.currentGraph().nodes.find((node) => node.id === menu.sourceId);
    const sourceType = source ? this.outputOf(source) : null;
    const input = kind.inputs.find((socket) => socket.type === sourceType);
    if (!source || !input) return;

    const before = this.snapshot();
    const id = this.nextNodeId(kind.kind.toLowerCase());
    const node: NodeDto = {
      id,
      kind: kind.kind,
      block: source.block,
      parameters: defaultParameters(kind.kind),
    };
    const links = menu.link
      ? this.currentGraph().links.filter((link) => link !== menu.link)
      : this.currentGraph().links;
    const insertedLinks: LinkDto[] = [{ from: source.id, to: id, input: input.name }];
    if (menu.link) insertedLinks.push({ from: id, to: menu.link.to, input: menu.link.input });
    const graph = {
      nodes: [...this.currentGraph().nodes, node],
      links: [...links, ...insertedLinks],
    };
    const layout = {
      ...this.currentLayout(),
      positions: {
        ...this.currentLayout().positions,
        [id]: snapPosition({ x: menu.x, y: menu.y }),
      },
    };

    this.remember(before);
    this.currentGraph.set(graph);
    this.currentLayout.set(layout);
    this.wireSource.set(null);
    this.compatibleMenu.set(null);
    this.select.emit(id);
    this.graphChange.emit(cloneGraph(graph));
    this.layoutChange.emit(cloneLayout(layout));
  }

  protected problemOf(nodeId: string): DraftProblemDto | null {
    const problems = this.problems().filter((problem) => problem.nodeId === nodeId);
    return problems.find((problem) => problem.severity === 'Error') ?? problems[0] ?? null;
  }

  @HostListener('document:keydown', ['$event'])
  protected keyboardHistory(event: KeyboardEvent): void {
    if (!(event.ctrlKey || event.metaKey) || event.key.toLowerCase() !== 'z') return;

    event.preventDefault();
    if (event.shiftKey) this.redo();
    else this.undo();
  }

  private moveNode(event: PointerEvent): void {
    if (!this.drag) return;

    const x = this.drag.start.x + event.clientX - this.drag.pointerX;
    const y = this.drag.start.y + event.clientY - this.drag.pointerY;
    const bounded = {
      x: Math.max(0, Math.min(x, this.drag.bounds.width - 240)),
      y: Math.max(0, Math.min(y, this.drag.bounds.height - 120)),
    };
    const position = snapPosition(bounded);

    this.currentLayout.update((layout) => ({
      ...layout,
      positions: { ...layout.positions, [this.drag!.nodeId]: position },
    }));
  }

  private stopDragging(): void {
    if (!this.drag) return;

    const moved = this.position(this.drag.nodeId);
    const changed = moved.x !== this.drag.start.x || moved.y !== this.drag.start.y;
    if (changed) {
      this.remember(this.drag.before);
      this.layoutChange.emit(cloneLayout(this.currentLayout()));
    }

    this.drag = null;
    this.removeDragListeners();
  }

  private removeDragListeners(): void {
    window.removeEventListener('pointermove', this.moveListener);
    window.removeEventListener('pointerup', this.upListener);
  }

  private undo(): void {
    const previous = this.undoStack.pop();
    if (!previous) return;

    this.redoStack.push(this.snapshot());
    this.restore(previous);
  }

  private redo(): void {
    const next = this.redoStack.pop();
    if (!next) return;

    this.undoStack.push(this.snapshot());
    this.restore(next);
  }

  private restore(snapshot: EditorSnapshot): void {
    this.currentGraph.set(cloneGraph(snapshot.graph));
    this.currentLayout.set(cloneLayout(snapshot.layout));
    this.graphChange.emit(cloneGraph(snapshot.graph));
    this.layoutChange.emit(cloneLayout(snapshot.layout));
  }

  private remember(snapshot: EditorSnapshot): void {
    this.undoStack.push(snapshot);
    this.redoStack.length = 0;
  }

  private snapshot(): EditorSnapshot {
    return { graph: cloneGraph(this.currentGraph()), layout: cloneLayout(this.currentLayout()) };
  }

  private nextNodeId(prefix: string): string {
    let number = 1;
    let id = `${prefix}-${number}`;
    const used = new Set(this.currentGraph().nodes.map((node) => node.id));
    while (used.has(id)) id = `${prefix}-${++number}`;

    return id;
  }
}

function defaultParameters(kind: NodeDto['kind']): Record<string, never | string | number> {
  switch (kind) {
    case 'Compare':
      return { operator: 'gt', value: 0 };
    case 'Between':
      return { low: 0, high: 1 };
    case 'Crossed':
      return { direction: 'up', level: 0, withinDays: 1 };
    default:
      return {};
  }
}
