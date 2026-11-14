import { ApiSchema } from '../../../core/api/api-schema';
import { JsonObject } from '../../../shared/models/json-value';
import { ReportMode } from '../../reports/models/report-schedule-dto';

export type StrategyStatus = 'Active' | 'Archived';

export type StrategyVersion = Omit<ApiSchema<'StrategyVersionDto'>, 'parameters'> & {
  parameters: JsonObject;
};

export type StrategyReport = Omit<ApiSchema<'StrategyReportDto'>, 'mode'> & {
  mode: ReportMode;
};

export type Strategy = Omit<ApiSchema<'StrategyDto'>, 'status' | 'version' | 'reports'> & {
  status: StrategyStatus;
  version: StrategyVersion;
  reports: StrategyReport[];
};

export type ReportDefinition = Omit<ApiSchema<'ReportDefinitionDto'>, 'mode'> & {
  mode: ReportMode;
};

export type StrategyType = Omit<
  ApiSchema<'StrategyTypeDto'>,
  'parametersSchema' | 'defaultParameters' | 'reports'
> & {
  parametersSchema: Record<string, unknown>;
  defaultParameters: JsonObject | null;
  reports: ReportDefinition[];
};

export type StrategySetupDraft = Omit<ApiSchema<'StrategySetupDraft'>, 'parameters'> & {
  parameters: JsonObject;
};
