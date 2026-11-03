import { formatPercent } from '../../../shared/pipes/format';
import { ReportTiming, scheduleLabel } from '../../reports/models/report-schedule';
import { SignalConditionDto, SignalMetric, SignalTargetKind } from './signal-dto';

export const metricLabels: Record<SignalMetric, string> = {
  DrawdownFromPeak: 'Просадка від піку',
  DayFall: 'Падіння за день',
  LossFromCost: 'Збиток від ціни купівлі',
  Level: 'Рівень значення',
};

export const builtInSignalKindLabels: Record<string, string> = {
  'volatility-index': 'Індекс волатильності',
  'account-drawdown': 'Просадка рахунку',
  'position-drawdown': 'Просадка позиції',
};

export const targetChoices: { label: string; value: SignalTargetKind }[] = [
  { label: 'Інструмент або група', value: 'Instrument' },
  { label: 'Кожна позиція рахунку', value: 'EachPosition' },
  { label: 'Під-портфель', value: 'SubPortfolio' },
  { label: 'Рахунок', value: 'Account' },
];

const clearReasonLabels: Record<string, string> = {
  Recovered: 'відновилось',
  PositionClosed: 'позицію закрито',
  Reconfigured: 'правило змінено',
  Disabled: 'сигнал вимкнено',
  Deleted: 'сигнал видалено',
};

const afterNewDataLabel = 'Після оновлення даних';

export function conditionLabel(condition: SignalConditionDto): string {
  if (condition.metric === 'Level') {
    const direction = condition.direction === 'Above' ? 'вище' : 'нижче';

    return `${direction} ${condition.threshold}`;
  }

  return `${metricLabels[condition.metric].toLowerCase()} ≥ ${percentLabel(condition.threshold)}`;
}

export function valueLabel(metric: SignalMetric, value: number): string {
  return metric === 'Level' ? levelLabel(value) : `−${formatPercent(value)}`;
}

function levelLabel(value: number): string {
  return new Intl.NumberFormat('uk-UA', { maximumFractionDigits: 2 }).format(value);
}

function percentLabel(fraction: number): string {
  const isWholePercent = Math.round(fraction * 1000) % 10 === 0;

  return formatPercent(fraction, isWholePercent ? 0 : 1);
}

export function signalScheduleLabel(schedule: ReportTiming): string {
  if (schedule.frequency.toLowerCase() === 'off') return afterNewDataLabel;

  const label = scheduleLabel(schedule);

  return label[0].toUpperCase() + label.slice(1);
}

export function clearReasonLabel(reason: string | null): string {
  return (reason && clearReasonLabels[reason]) || '';
}
