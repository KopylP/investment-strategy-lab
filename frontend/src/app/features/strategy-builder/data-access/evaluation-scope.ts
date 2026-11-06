import { DraftSetupDto, GraphDto, NodeDto } from '../models/draft-dto';
import { resolvedSeries } from './setup-bindings';

export type EvaluationScope = 'per-fund' | 'shared' | 'portfolio' | null;

export function evaluationScope(
  node: NodeDto,
  graph: GraphDto,
  setup: DraftSetupDto,
): EvaluationScope {
  if (node.kind === 'Setup') return null;
  if (['SelectionResult', 'FundSelection', 'Cash', 'Portfolio'].includes(node.kind)) return 'portfolio';
  if (['EntryResult', 'ExitResult', 'RiskResult'].includes(node.kind)) return 'per-fund';
  if (node.kind === 'MarketResult') return 'shared';
  const seen = new Set<string>();
  function dependsOnFund(current: NodeDto): boolean {
    if (seen.has(current.id)) return false;
    seen.add(current.id);
    if (current.kind === 'Indicator') {
      const series = resolvedSeries(current, graph, setup);
      return (
        !!series &&
        typeof series === 'object' &&
        !Array.isArray(series) &&
        series['series'] === 'fund'
      );
    }
    if (current.kind === 'Signal') return current.parameters['scope'] === 'thisFund';
    return graph.links
      .filter((link) => link.to === current.id && link.input !== 'funds' && link.input !== 'budget')
      .some((link) => {
        const parent = graph.nodes.find((item) => item.id === link.from);
        return !!parent && dependsOnFund(parent);
      });
  }
  return dependsOnFund(node) ? 'per-fund' : 'shared';
}
