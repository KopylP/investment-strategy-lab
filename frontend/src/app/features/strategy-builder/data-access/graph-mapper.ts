import { BlockKind, GraphDto, LayoutDto, NodeDto, NodePosition } from '../models/draft-dto';

const gridSize = 16;
const columnWidth = 256;
const rowHeight = 144;

export function graphWithNode(graph: GraphDto, node: NodeDto): GraphDto {
  return {
    nodes: graph.nodes.map((current) => (current.id === node.id ? node : current)),
    links: [...graph.links],
  };
}

export function positionsForMissingNodes(graph: GraphDto, layout: LayoutDto): LayoutDto {
  const positions = { ...layout.positions };

  for (const block of blockKinds()) {
    const missing = graph.nodes.filter((node) => node.block === block && !positions[node.id]);
    const occupied = graph.nodes.filter(
      (node) => node.block === block && positions[node.id],
    ).length;

    missing.forEach((node, index) => {
      positions[node.id] = arrangedPosition(occupied + index, isResultNode(node));
    });
  }

  return { ...layout, positions, collapsed: [...layout.collapsed] };
}

export function arrangedBlock(graph: GraphDto, layout: LayoutDto, block: BlockKind): LayoutDto {
  const positions = { ...layout.positions };
  const nodes = graph.nodes.filter((node) => node.block === block);
  const ordinary = nodes.filter((node) => !isResultNode(node));
  const results = nodes.filter(isResultNode);

  ordinary.forEach((node, index) => (positions[node.id] = arrangedPosition(index, false)));
  results.forEach(
    (node, index) => (positions[node.id] = { x: 800, y: gridSize + index * rowHeight }),
  );

  return { ...layout, positions, collapsed: [...layout.collapsed] };
}

export function snapPosition(position: NodePosition): NodePosition {
  return {
    x: Math.round(position.x / gridSize) * gridSize,
    y: Math.round(position.y / gridSize) * gridSize,
  };
}

export function cloneGraph(graph: GraphDto): GraphDto {
  return structuredClone(graph);
}

export function cloneLayout(layout: LayoutDto): LayoutDto {
  return structuredClone(layout);
}

function arrangedPosition(index: number, result: boolean): NodePosition {
  if (result) return { x: 800, y: gridSize };

  return {
    x: gridSize + (index % 3) * columnWidth,
    y: gridSize + Math.floor(index / 3) * rowHeight,
  };
}

function isResultNode(node: NodeDto): boolean {
  return node.kind.endsWith('Result');
}

function blockKinds(): BlockKind[] {
  return ['MarketFilter', 'Exit', 'Entry', 'Selection', 'Risk'];
}
