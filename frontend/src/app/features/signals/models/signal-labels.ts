import { translate } from '@jsverse/transloco';
import { formatPercent } from '../../../shared/pipes/format';
import { ReportTiming, scheduleLabel } from '../../reports/models/report-schedule';
import { SignalConditionDto, SignalMetric, SignalTargetKind } from './signal-dto';
import { activeLocale } from '../../../core/i18n/language';

export const metricLabels: Record<SignalMetric, string> = {
  DrawdownFromPeak: translate('metric.DrawdownFromPeak'),
  DayFall: translate('metric.DayFall'),
  LossFromCost: translate('metric.LossFromCost'),
  Level: translate('metric.Level'),
};

export const builtInSignalKindLabels: Record<string, string> = {
  'volatility-index': translate('builtIn.volatilityIndex'),
  'account-drawdown': translate('builtIn.accountDrawdown'),
  'position-drawdown': translate('builtIn.positionDrawdown'),
};

export const targetChoices: { label: string; value: SignalTargetKind }[] = [
  { label: translate('target.Instrument'), value: 'Instrument' },
  { label: translate('target.EachPosition'), value: 'EachPosition' },
  { label: translate('target.SubPortfolio'), value: 'SubPortfolio' },
  { label: translate('target.Account'), value: 'Account' },
];

const clearReasonLabels: Record<string, string> = {
  Recovered: translate('cleared.Recovered'),
  PositionClosed: translate('cleared.PositionClosed'),
  Reconfigured: translate('cleared.Reconfigured'),
  Disabled: translate('cleared.Disabled'),
  Deleted: translate('cleared.Deleted'),
};

const afterNewDataLabel = translate('signals.editor.frequency.off');

export function conditionLabel(condition: SignalConditionDto): string {
  if (condition.metric === 'Level') {
    const direction = condition.direction === 'Above' ? translate('signals.labels.above') : translate('signals.labels.below');

    return `${direction} ${condition.threshold}`;
  }

  return `${metricLabels[condition.metric].toLowerCase()} ≥ ${percentLabel(condition.threshold)}`;
}

export function valueLabel(metric: SignalMetric, value: number): string {
  return metric === 'Level' ? levelLabel(value) : `−${formatPercent(value)}`;
}

function levelLabel(value: number): string {
  return new Intl.NumberFormat(activeLocale, { maximumFractionDigits: 2 }).format(value);
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
