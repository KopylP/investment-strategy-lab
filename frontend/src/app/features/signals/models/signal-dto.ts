import { ApiSchema } from '../../../core/api/api-schema';
import { ReportTiming } from '../../reports/models/report-schedule';

export type SignalTargetKind =
  | 'Instrument'
  | 'InstrumentGroup'
  | 'EachPosition'
  | 'SubPortfolio'
  | 'Account';
export type SignalMetric = 'DrawdownFromPeak' | 'DayFall' | 'LossFromCost' | 'Level';
export type SignalDirection = 'Above' | 'Below';

export type SignalConditionDto = Omit<ApiSchema<'SignalConditionDto'>, 'metric' | 'direction'> & {
  metric: SignalMetric;
  direction: SignalDirection;
};

export type SignalDto = Omit<ApiSchema<'SignalDto'>, 'target' | 'condition' | 'schedule'> & {
  target: SignalTargetKind;
  condition: SignalConditionDto;
  schedule: ReportTiming;
};

export type SignalOccurrenceDto = Omit<ApiSchema<'SignalOccurrenceDto'>, 'metric'> & {
  metric: SignalMetric;
};

export type SignalsOverviewDto = Omit<ApiSchema<'SignalsOverviewDto'>, 'signals' | 'active' | 'recent'> & {
  signals: SignalDto[];
  active: SignalOccurrenceDto[];
  recent: SignalOccurrenceDto[];
};

export type CreateSignalBody = Omit<ApiSchema<'CreateSignalCommand'>, 'target' | 'condition'> & {
  target: SignalTargetKind;
  condition: SignalConditionDto;
};

export type ChangeSignalBody = Omit<ApiSchema<'ChangeSignalCommand'>, 'condition'> & {
  condition: SignalConditionDto;
};

export type SignalCreatedDto = ApiSchema<'SignalCreatedDto'>;
export type SignalReportRuleDto = ApiSchema<'SignalReportRuleDto'>;
