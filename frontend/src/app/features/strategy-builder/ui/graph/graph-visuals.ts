import { NodeDto } from '../../models/draft-dto';
import { SocketType } from '../../models/vocabulary-dto';

// Node appearance describes its job; wire appearance describes the value it carries.
export const nodeStyles = {
  indicator: { label: 'Індикатор', icon: 'pi-chart-line', color: '#2563eb' },
  condition: { label: 'Умова', icon: 'pi-filter', color: '#b45309' },
  logic: { label: 'Логіка', icon: 'pi-sitemap', color: '#7c3aed' },
  signal: { label: 'Сигнал', icon: 'pi-bolt', color: '#be185d' },
  result: { label: 'Результат', icon: 'pi-flag', color: '#0f766e' },
};

export function nodeStyle(node: NodeDto) {
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
  Number: { label: 'Число', color: '#2563eb', dash: '', width: 2 },
  Boolean: { label: 'Умова: так / ні', color: '#7c3aed', dash: '7 5', width: 2 },
  Funds: { label: 'Відібрані фонди', color: '#0f766e', dash: '', width: 4 },
};
