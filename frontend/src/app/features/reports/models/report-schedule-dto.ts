import { ApiSchema } from '../../../core/api/api-schema';

export type ReportMode = 'rebalance' | 'riskReview';

export type ReportTiming = ApiSchema<'ReportTimingDto'>;

export type ScheduledReport = Omit<ApiSchema<'ScheduledReportDto'>, 'mode'> & {
  mode: ReportMode;
};

export type ReportSchedule = Omit<ApiSchema<'ReportScheduleDto'>, 'reports'> & {
  reports: ScheduledReport[];
};

export type ChangeReportScheduleBody = ApiSchema<'ChangeReportScheduleCommand'>;
