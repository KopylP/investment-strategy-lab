import { ReportTiming } from '../../reports/models/report-schedule';

export type SignalTargetKind = 'Instrument' | 'EachPosition' | 'SubPortfolio' | 'Account';
export type SignalMetric = 'DrawdownFromPeak' | 'DayFall' | 'LossFromCost' | 'Level';
export type SignalDirection = 'Above' | 'Below';

export interface SignalConditionDto {
  metric: SignalMetric;
  direction: SignalDirection;
  threshold: number;
  rearmAt: number | null;
  windowDays: number | null;
}

export interface SignalDto {
  id: string;
  name: string;
  target: SignalTargetKind;
  accountId: string | null;
  subPortfolioId: string | null;
  instrumentId: string | null;
  condition: SignalConditionDto;
  schedule: ReportTiming;
  enabled: boolean;
  builtIn: boolean;
  lastCheckedAt: string | null;
  activeCount: number;
}

export interface SignalOccurrenceDto {
  id: string;
  signalId: string;
  signalName: string;
  metric: SignalMetric;
  subjectLabel: string;
  triggerValue: number;
  latestValue: number;
  triggerDate: string;
  latestDate: string;
  triggeredAt: string;
  clearedAt: string | null;
  clearReason: string | null;
}

export interface SignalsOverviewDto {
  signals: SignalDto[];
  active: SignalOccurrenceDto[];
  recent: SignalOccurrenceDto[];
}

export interface CreateSignalBody {
  name: string;
  target: SignalTargetKind;
  accountId: string | null;
  subPortfolioId: string | null;
  instrumentId: string | null;
  condition: SignalConditionDto;
  schedule: ReportTiming;
}

export interface ChangeSignalBody {
  name: string;
  condition: SignalConditionDto;
  schedule: ReportTiming;
  enabled: boolean;
}

export interface SignalReportRuleDto {
  id: string;
  signalId: string;
  accountId: string;
  reportCode: string;
  problem: string | null;
}
