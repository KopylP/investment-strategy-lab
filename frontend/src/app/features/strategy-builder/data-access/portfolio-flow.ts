import { JsonValue } from '../../../shared/models/json-value';
import { GraphDto, NodeDto } from '../models/draft-dto';
import { setupNodeId } from './setup-bindings';

export const portfolioNodeId = 'strategy-portfolio';
export const cashNodeId = 'strategy-cash';
export const fundSelectionId = 'fund-selection';
export const selectionFieldNames = [
  'top',
  'order',
  'maxPerGroup',
  'maxCorrelation',
  'correlationDays',
];

export function withPortfolioFlow(graph: GraphDto): GraphDto {
  const legacy = !graph.nodes.some((node) => node.kind === 'Cash' || node.kind === 'FundSelection');
  const weights = graph.nodes.find((node) => node.kind === 'SelectionResult');
  const needsSplit =
    weights &&
    (selectionFieldNames.some((key) => Object.hasOwn(weights.parameters, key)) ||
      graph.links.some((link) => link.to === weights.id && link.input === 'rankBy'));
  const oldCash = graph.links.some((link) => link.from === setupNodeId && link.output === 'cashId');
  const oldUniverse = graph.links.some(
    (link) => link.from === setupNodeId && link.output === 'universe',
  );
  const needsPortfolio = !graph.nodes.some((node) => node.kind === 'Portfolio');
  const needsCash = !graph.nodes.some((node) => node.id === cashNodeId);
  if (!legacy && !needsSplit && !oldCash && !oldUniverse && !needsPortfolio && !needsCash)
    return graph;
  let nodes = [...graph.nodes];
  let links = graph.links.map((link) =>
    link.from === setupNodeId && link.output === 'cashId'
      ? { ...link, from: cashNodeId, output: 'series' }
      : link.from === setupNodeId && link.output === 'universe'
        ? { ...link, output: 'funds' }
        : link,
  );
  if (!nodes.some((node) => node.id === cashNodeId))
    nodes.push({ id: cashNodeId, kind: 'Cash', block: 'Shared', parameters: {} });
  if (weights && (legacy || needsSplit)) {
    const existing = nodes.find((node) => node.id === fundSelectionId);
    const parameters: Record<string, JsonValue> = {
      top: 3,
      order: 'desc',
      ...existing?.parameters,
    };
    for (const key of selectionFieldNames)
      if (Object.hasOwn(weights.parameters, key)) parameters[key] = weights.parameters[key];
    nodes = nodes
      .filter((node) => node.id !== fundSelectionId)
      .map((node) =>
        node.id === weights.id
          ? {
              ...node,
              parameters: Object.fromEntries(
                Object.entries(node.parameters).filter(
                  ([key]) => !selectionFieldNames.includes(key),
                ),
              ),
            }
          : node,
      );
    nodes.push({ id: fundSelectionId, kind: 'FundSelection', block: 'Shared', parameters });
    links = links.map((link) =>
      link.to === weights.id && link.input === 'rankBy' ? { ...link, to: fundSelectionId } : link,
    );
  }
  if (legacy && weights) {
    const entry = nodes.find((node) => node.kind === 'EntryResult'),
      exit = nodes.find((node) => node.kind === 'ExitResult'),
      market = nodes.find((node) => node.kind === 'MarketResult');
    if (entry && exit && market)
      links.push(
        { from: setupNodeId, output: 'funds', to: exit.id, input: 'funds' },
        { from: exit.id, to: entry.id, input: 'funds' },
        { from: entry.id, to: fundSelectionId, input: 'funds' },
        { from: fundSelectionId, to: weights.id, input: 'funds' },
        { from: market.id, to: weights.id, input: 'budget' },
        { from: weights.id, output: 'cash', to: cashNodeId, input: 'remainder' },
      );
  }
  if (needsPortfolio) {
    nodes.push({ id: portfolioNodeId, kind: 'Portfolio', block: 'Shared', parameters: {} });
    const risk = nodes.find((node) => node.kind === 'RiskResult');
    if (weights) links.push({ from: weights.id, to: portfolioNodeId, input: 'funds' });
    links.push({ from: cashNodeId, to: portfolioNodeId, input: 'cash' });
    if (risk) links.push({ from: risk.id, to: portfolioNodeId, input: 'risk' });
  }
  return { nodes, links };
}

export function candidatePath(graph: GraphDto): NodeDto[] {
  const path: NodeDto[] = [],
    seen = new Set<string>();
  let node = graph.nodes.find((node) => node.kind === 'SelectionResult');
  while (node && !seen.has(node.id)) {
    path.push(node);
    seen.add(node.id);
    const link = graph.links.find((link) => link.to === node!.id && link.input === 'funds');
    node = graph.nodes.find((item) => item.id === link?.from);
  }
  return path;
}
