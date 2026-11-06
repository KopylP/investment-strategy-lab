import { GraphDto, NodeDto } from '../models/draft-dto';

export function dependenciesOf(graph: GraphDto, id: string): Set<string> {
  const visited = new Set<string>(),
    pending = [id];
  while (pending.length) {
    const current = pending.pop()!;
    if (visited.has(current)) continue;
    visited.add(current);
    if (graph.nodes.find((node) => node.id === current)?.kind === 'Cash') continue;
    pending.push(...graph.links.filter((link) => link.to === current).map((link) => link.from));
  }
  return visited;
}

export function resultsUsing(graph: GraphDto, id: string): NodeDto[] {
  const visited = new Set<string>(),
    pending = [id],
    results: NodeDto[] = [];
  while (pending.length) {
    const current = pending.pop()!;
    if (visited.has(current)) continue;
    visited.add(current);
    const node = graph.nodes.find((node) => node.id === current);
    if (node?.kind.endsWith('Result')) {
      results.push(node);
      continue;
    }
    pending.push(...graph.links.filter((link) => link.from === current).map((link) => link.to));
  }
  return results;
}

export function fundContext(node: NodeDto): boolean {
  const of = node.parameters['of'];
  return (
    (node.kind === 'Indicator' &&
      !!of &&
      typeof of === 'object' &&
      !Array.isArray(of) &&
      of['series'] === 'fund') ||
    (node.kind === 'Signal' && node.parameters['scope'] === 'thisFund')
  );
}
