import { BlockKind, GraphDto, NodeDto } from '../models/draft-dto';

export interface ConditionRows {
  junction: NodeDto | null;
  conditions: NodeDto[];
}

const rowKinds = new Set(['Compare', 'Between', 'Crossed', 'Signal']);

export function conditionRows(graph: GraphDto, block: BlockKind): ConditionRows | null {
  const result = graph.nodes.find((node) => node.block === block && node.kind.endsWith('Result'));
  if (!result) return null;

  const roots = graph.links.filter((link) => link.to === result.id);
  if (roots.length !== 1) return null;

  const root = graph.nodes.find((node) => node.id === roots[0].from);
  if (!root || root.block !== block) return null;
  if (rowKinds.has(root.kind)) return { junction: null, conditions: [root] };
  if (root.kind !== 'All' && root.kind !== 'Any') return null;

  const links = graph.links.filter((link) => link.to === root.id);
  const children = links.map((link) => graph.nodes.find((node) => node.id === link.from));
  const simple = children.every((node) => node && node.block === block && rowKinds.has(node.kind));
  if (!simple || !children.length) return null;

  return { junction: root, conditions: children as NodeDto[] };
}
