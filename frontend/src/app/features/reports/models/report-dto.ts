import { ApiSchema } from '../../../core/api/api-schema';

export type ReportStatus = 'New' | 'InProgress' | 'Closed' | 'Superseded';
export type ReportKind = 'Monthly' | 'Recalculation' | 'RiskReview';
export type ReportTrigger = 'Scheduled' | 'OnDemand' | 'Signal';
export type ActionKind = 'Contribute' | 'Sell' | 'Unpark' | 'Transfer' | 'Buy' | 'Park';
export type ActionStatus = 'Pending' | 'PartiallyExecuted' | 'Executed' | 'Skipped';
export type AllocationRole = 'Pick' | 'Exit' | 'Cash';
export type ScoreValue = number | boolean | string | null;

export type ReportSummaryDto = Omit<
  ApiSchema<'ReportSummaryDto'>,
  'kind' | 'trigger' | 'status'
> & {
  kind: ReportKind;
  trigger: ReportTrigger;
  status: ReportStatus;
};

export type ReportRequestDto = Omit<ApiSchema<'ReportRequestDto'>, 'kind' | 'trigger'> & {
  kind: ReportKind;
  trigger: ReportTrigger;
};

export type FillDto = ApiSchema<'FillDto'>;

export type ReportActionDto = Omit<ApiSchema<'ReportActionDto'>, 'kind' | 'status'> & {
  kind: ActionKind;
  status: ActionStatus;
};

export type SectionScoreDto = Omit<ApiSchema<'SectionScoreDto'>, 'values'> & {
  values: Record<string, ScoreValue> | null;
};

export type SectionColumnDto = ApiSchema<'SectionColumnDto'>;

export type SectionDecisionDto = Omit<ApiSchema<'SectionDecisionDto'>, 'scores'> & {
  scores: SectionScoreDto[];
};

export type SectionOrderDto = ApiSchema<'SectionOrderDto'>;

export type SectionAllocationDto = Omit<ApiSchema<'SectionAllocationDto'>, 'role'> & {
  role: AllocationRole;
};

export type SectionRiskDto = ApiSchema<'SectionRiskDto'>;

export type ReportSectionDto = Omit<ApiSchema<'ReportSectionDto'>, 'decision' | 'allocation'> & {
  decision: SectionDecisionDto | null;
  allocation: SectionAllocationDto[];
};

export type PlanReviewDto = ApiSchema<'PlanReviewDto'>;
export type TaxEstimateDto = ApiSchema<'TaxEstimateDto'>;

export type ReportDetailsDto = Omit<
  ApiSchema<'ReportDetailsDto'>,
  'summary' | 'actions' | 'sections'
> & {
  summary: ReportSummaryDto;
  actions: ReportActionDto[];
  sections: ReportSectionDto[];
};

export type MatchedOperationDto = ApiSchema<'MatchedOperationDto'>;
export type LoadOperationsPreviewDto = ApiSchema<'LoadOperationsPreviewDto'>;
export type PlanSettingsDto = ApiSchema<'PlanSettingsDto'>;
export type ReportRequestedDto = ApiSchema<'ReportRequestedResponse'>;
export type GenerateReportBody = ApiSchema<'GenerateReportRequest'>;
export type MarkActionExecutedBody = ApiSchema<'MarkActionExecutedCommand'>;
export type RecordPartialFillBody = ApiSchema<'RecordPartialFillCommand'>;
export type SkipActionBody = ApiSchema<'SkipActionCommand'>;
export type StatementImportDto = ApiSchema<'StatementImportDto'>;
export type UploadStatementBody = ApiSchema<'UploadStatementRequest'>;
export type IngestReport = ApiSchema<'IngestReport'>;
