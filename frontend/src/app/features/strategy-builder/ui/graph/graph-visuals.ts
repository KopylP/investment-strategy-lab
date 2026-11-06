import { NodeDto } from '../../models/draft-dto';
import { SocketType } from '../../models/vocabulary-dto';

// Node appearance describes its job; wire appearance describes the value it carries.
export const nodeStyles = {
  setup: { label: 'Фонди й ряди', icon: 'pi-sliders-h', color: '#64748b' },
  selection: { label: 'Відібрати фонди', icon: 'pi-filter', color: '#0f766e' },
  portfolio: { label: 'Цільовий портфель', icon: 'pi-briefcase', color: '#0f766e' },
  cash: { label: 'Кеш', icon: 'pi-wallet', color: '#64748b' },
  indicator: { label: 'Індикатор', icon: 'pi-chart-line', color: '#2563eb' },
  condition: { label: 'Умова', icon: 'pi-filter', color: '#b45309' },
  logic: { label: 'Логіка', icon: 'pi-sitemap', color: '#7c3aed' },
  signal: { label: 'Сигнал', icon: 'pi-bolt', color: '#be185d' },
  result: { label: 'Результат', icon: 'pi-flag', color: '#0f766e' },
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
  Series: { label: 'Ціновий ряд', color: '#0e7490', dash: '', width: 2 },
  SeriesList: { label: 'Набір рядів', color: '#64748b', dash: '3 3', width: 3 },
  Number: { label: 'Число', color: '#2563eb', dash: '', width: 2 },
  Boolean: { label: 'Умова: так / ні', color: '#7c3aed', dash: '7 5', width: 2 },
  Funds: { label: 'Відібрані фонди', color: '#0f766e', dash: '', width: 2 },
  Candidates: { label: 'Кандидати', color: '#0f766e', dash: '', width: 2 },
  RiskPolicy: { label: 'Контроль ризику', color: '#b45309', dash: '3 5', width: 2 },
  Budget: { label: 'Бюджет', color: '#64748b', dash: '4 3', width: 3 },
};
