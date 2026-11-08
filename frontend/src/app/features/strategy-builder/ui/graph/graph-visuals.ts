import { translate } from '@jsverse/transloco';
import { NodeDto } from '../../models/draft-dto';
import { SocketType } from '../../models/vocabulary-dto';

// Node appearance describes its job; wire appearance describes the value it carries.
export const nodeStyles = {
  setup: { label: translate('builder.graph.visual.setup'), icon: 'pi-sliders-h', color: '#64748b' },
  selection: {
    label: translate('builder.graph.visual.selectFunds'),
    icon: 'pi-filter',
    color: '#0f766e',
  },
  portfolio: {
    label: translate('builder.graph.visual.targetPortfolio'),
    icon: 'pi-briefcase',
    color: '#0f766e',
  },
  cash: { label: translate('common.cash'), icon: 'pi-wallet', color: '#64748b' },
  indicator: {
    label: translate('builder.graph.visual.indicator'),
    icon: 'pi-chart-line',
    color: '#2563eb',
  },
  condition: {
    label: translate('builder.graph.visual.condition'),
    icon: 'pi-filter',
    color: '#b45309',
  },
  logic: { label: translate('builder.graph.visual.logic'), icon: 'pi-sitemap', color: '#7c3aed' },
  signal: { label: translate('builder.graph.visual.signal'), icon: 'pi-bolt', color: '#be185d' },
  result: { label: translate('builder.graph.visual.result'), icon: 'pi-flag', color: '#0f766e' },
};

export function nodeStyle(node: NodeDto) {
  if (node.kind === 'Setup') return nodeStyles.setup;
  if (node.kind === 'FundSelection') return nodeStyles.selection;
  if (node.kind === 'Portfolio') return nodeStyles.portfolio;
  if (node.kind === 'Cash') return nodeStyles.cash;
  if (node.kind.endsWith('Result')) return nodeStyles.result;
  if (node.kind === 'Indicator') return nodeStyles.indicator;
  if (node.kind === 'Signal') return nodeStyles.signal;
  if (['All', 'Any', 'Not'].includes(node.kind)) return nodeStyles.logic;
  return nodeStyles.condition;
}

export const wireStyles: Record<
  SocketType,
  { label: string; color: string; dash: string; width: number }
> = {
  Series: {
    label: translate('builder.graph.visual.priceSeries'),
    color: '#0e7490',
    dash: '',
    width: 2,
  },
  SeriesList: {
    label: translate('builder.graph.visual.seriesSet'),
    color: '#64748b',
    dash: '3 3',
    width: 3,
  },
  Number: { label: translate('builder.graph.visual.number'), color: '#2563eb', dash: '', width: 2 },
  Boolean: {
    label: translate('builder.graph.visual.yesNo'),
    color: '#7c3aed',
    dash: '7 5',
    width: 2,
  },
  Funds: {
    label: translate('builder.graph.visual.selectedFunds'),
    color: '#0f766e',
    dash: '',
    width: 2,
  },
  Candidates: {
    label: translate('builder.graph.visual.candidates'),
    color: '#0f766e',
    dash: '',
    width: 2,
  },
  RiskPolicy: {
    label: translate('builder.graph.visual.riskControl'),
    color: '#b45309',
    dash: '3 5',
    width: 2,
  },
  Budget: {
    label: translate('builder.graph.visual.budget'),
    color: '#64748b',
    dash: '4 3',
    width: 3,
  },
};
