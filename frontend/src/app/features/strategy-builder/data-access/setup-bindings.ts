import { translate } from '@jsverse/transloco';
import { JsonValue } from '../../../shared/models/json-value';
import { DraftSetupDto, GraphDto, NodeDto } from '../models/draft-dto';
export const setupNodeId = 'strategy-setup';
export const setupFields = [
  { key: 'funds', label: translate('builder.graph.node.funds'), type: 'Series' as const },
  { key: 'benchmarkId', label: translate('strategies.detail.benchmark'), type: 'Series' as const },
  { key: 'cashId', label: translate('builder.graph.node.series.cash'), type: 'Series' as const },
  {
    key: 'volatilityIndexId',
    label: translate('strategies.detail.volatilityIndex'),
    type: 'Series' as const,
  },
  {
    key: 'extraSeries',
    label: translate('builder.setup.extraSeries'),
    type: 'SeriesList' as const,
  },
];
export function setupOutput(node: NodeDto): string | null {
  const of = node.parameters['of'];
  if (!of || typeof of !== 'object' || Array.isArray(of)) return null;
  return (
    (
      {
        fund: 'funds',
        benchmark: 'benchmarkId',
        cash: 'cashId',
        volatilityIndex: 'volatilityIndexId',
      } as Record<string, string>
    )[String(of['series'])] ??
    (of['series'] === 'extra' && typeof of['instrumentId'] === 'string'
      ? `extra:${of['instrumentId']}`
      : null)
  );
}
export function withSetup(graph: GraphDto): GraphDto {
  if (graph.nodes.some((node) => node.id === setupNodeId)) return graph;
  const nodes = graph.nodes.some((node) => node.id === setupNodeId)
    ? graph.nodes
    : [
        { id: setupNodeId, kind: 'Setup' as const, block: 'Shared' as const, parameters: {} },
        ...graph.nodes,
      ];
  const links = [...graph.links];
  for (const node of nodes) {
    if (
      node.kind !== 'Indicator' ||
      links.some((link) => link.to === node.id && link.input === 'of')
    )
      continue;
    const output = setupOutput(node);
    if (output) links.push({ from: setupNodeId, to: node.id, input: 'of', output });
  }
  return { nodes, links };
}
export function resolvedSeries(
  node: NodeDto,
  graph: GraphDto,
  setup: DraftSetupDto,
): JsonValue | undefined {
  const binding = graph.links.find(
    (link) =>
      link.to === node.id &&
      link.input === 'of' &&
      (link.from === setupNodeId || link.from === 'strategy-cash'),
  );
  const key =
    binding?.from === 'strategy-cash' && binding.output === 'series' ? 'cashId' : binding?.output;
  if (!key) return node.parameters['of'];
  if (key === 'funds') return { series: 'fund' };
  if (key === 'benchmarkId') return { series: 'benchmark' };
  if (key === 'volatilityIndexId') return { series: 'volatilityIndex' };
  if (key === 'cashId') return { series: 'extra', instrumentId: setup.cashId };
  if (key.startsWith('extra:')) return { series: 'extra', instrumentId: key.slice(6) };
  return undefined;
}
