import { translate } from '@jsverse/transloco';
import { NodeDto } from '../../models/draft-dto';
import { VocabularyDto } from '../../models/vocabulary-dto';

export function indicatorLabel(node: NodeDto, vocabulary: VocabularyDto): string {
  const code = textParameter(node, 'indicator');
  return (
    vocabulary.indicators.find((indicator) => indicator.code === code)?.label ??
    code ??
    translate<string>('builder.graph.visual.indicator')
  );
}

export function conditionLabel(node: NodeDto, vocabulary: VocabularyDto): string {
  const code = textParameter(node, 'kind');
  return (
    vocabulary.conditions.find((condition) => condition.code === code)?.label ?? code ?? node.kind
  );
}

export function seriesLabel(node: NodeDto): string {
  const value = node.parameters['of'];
  if (!value || typeof value !== 'object' || Array.isArray(value)) return '';

  const series = value['series'];
  if (series === 'fund') return translate('builder.graph.node.series.fund');
  if (series === 'benchmark') return translate('builder.graph.node.series.benchmark');
  if (series === 'volatilityIndex') return translate('builder.graph.node.series.volatilityIndex');
  if (series === 'cash') return translate('builder.graph.node.seriesLabel.cash');

  return typeof value['instrumentId'] === 'string' ? value['instrumentId'] : '';
}

export function parameterSummary(node: NodeDto): string {
  return Object.entries(node.parameters)
    .filter(([name]) => !['indicator', 'kind', 'of'].includes(name))
    .map(([name, value]) => `${name}: ${displayValue(value)}`)
    .join(' · ');
}

export function textParameter(node: NodeDto, name: string): string | null {
  const value = node.parameters[name];
  return typeof value === 'string' ? value : null;
}

function displayValue(value: unknown): string {
  if (typeof value === 'boolean') return value ? translate('common.yes') : translate('common.no');
  if (value === null) return '—';
  if (typeof value === 'object') return '';

  return String(value);
}
