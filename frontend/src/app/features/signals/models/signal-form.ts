import { editableTiming, isTimingComplete } from '../../reports/models/report-schedule';
import { ReportTiming } from '../../reports/models/report-schedule-dto';
import {
  CreateSignalBody,
  SignalConditionDto,
  SignalDirection,
  SignalDto,
  SignalMetric,
  SignalTargetKind,
} from './signal-dto';

const percentPrecision = 10_000;
const defaultPeakWindowDays = 365;

const afterNewData: ReportTiming = {
  frequency: 'off',
  day: 1,
  weekday: 'sunday',
  time: '20:00',
  minute: 0,
};

const metricsOfTarget: Record<SignalTargetKind, SignalMetric[]> = {
  Instrument: ['DrawdownFromPeak', 'DayFall', 'Level'],
  InstrumentGroup: ['DrawdownFromPeak', 'DayFall'],
  EachPosition: ['DrawdownFromPeak', 'DayFall', 'LossFromCost'],
  SubPortfolio: ['DrawdownFromPeak', 'DayFall'],
  Account: ['DrawdownFromPeak', 'DayFall'],
};

export interface SignalForm {
  name: string;
  target: SignalTargetKind;
  accountId: string | null;
  subPortfolioId: string | null;
  instrumentId: string | null;
  groupName: string | null;
  metric: SignalMetric;
  direction: SignalDirection;
  threshold: number | null;
  rearmAt: number | null;
  windowDays: number | null;
  schedule: ReportTiming;
  enabled: boolean;
}

export function watchesMarket(target: SignalTargetKind): boolean {
  return target === 'Instrument' || target === 'InstrumentGroup';
}

export function metricsOf(target: SignalTargetKind): SignalMetric[] {
  return metricsOfTarget[target];
}

export function emptyForm(accountId: string | null): SignalForm {
  return {
    name: '',
    target: 'EachPosition',
    accountId,
    subPortfolioId: null,
    instrumentId: null,
    groupName: null,
    metric: 'DrawdownFromPeak',
    direction: 'Above',
    threshold: 20,
    rearmAt: null,
    windowDays: defaultPeakWindowDays,
    schedule: afterNewData,
    enabled: true,
  };
}

export function formOf(signal: SignalDto): SignalForm {
  const { metric, direction, threshold, rearmAt, windowDays } = signal.condition;

  return {
    name: signal.name,
    target: signal.target,
    accountId: signal.accountId,
    subPortfolioId: signal.subPortfolioId,
    instrumentId: signal.instrumentId,
    groupName: signal.groupName,
    metric,
    direction,
    threshold: toDisplayed(metric, threshold),
    rearmAt: rearmAt === null ? null : toDisplayed(metric, rearmAt),
    windowDays,
    schedule: editableTiming(signal.schedule),
    enabled: signal.enabled,
  };
}

export function conditionOf(form: SignalForm): SignalConditionDto {
  return {
    metric: form.metric,
    direction: form.direction,
    threshold: toStored(form.metric, form.threshold ?? 0),
    rearmAt: form.rearmAt === null ? null : toStored(form.metric, form.rearmAt),
    windowDays: form.metric === 'DrawdownFromPeak' ? form.windowDays : null,
  };
}

export function createBodyOf(form: SignalForm): CreateSignalBody {
  return {
    name: form.name,
    target: form.target,
    accountId: watchesMarket(form.target) ? null : form.accountId,
    subPortfolioId: form.target === 'SubPortfolio' ? form.subPortfolioId : null,
    instrumentId: form.target === 'Instrument' ? form.instrumentId : null,
    groupName: form.target === 'InstrumentGroup' ? form.groupName : null,
    condition: conditionOf(form),
    schedule: form.schedule,
  };
}

export function isFormComplete(form: SignalForm): boolean {
  const hasNumbers = form.threshold !== null && form.threshold > 0;

  return (
    form.name.trim().length > 0 && hasNumbers && hasTarget(form) && isTimingComplete(form.schedule)
  );
}

function hasTarget(form: SignalForm): boolean {
  switch (form.target) {
    case 'Instrument':
      return form.instrumentId !== null;
    case 'InstrumentGroup':
      return form.groupName !== null;
    case 'SubPortfolio':
      return form.accountId !== null && form.subPortfolioId !== null;
    default:
      return form.accountId !== null;
  }
}

function toDisplayed(metric: SignalMetric, stored: number): number {
  if (metric === 'Level') return stored;

  return Math.round(stored * 100 * percentPrecision) / percentPrecision;
}

function toStored(metric: SignalMetric, displayed: number): number {
  return metric === 'Level' ? displayed : displayed / 100;
}
