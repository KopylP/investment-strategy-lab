import { JsonObject } from '../../../shared/ui/schema-form/schema-form';
import { ReportTiming } from '../../reports/models/report-schedule';

export interface StrategyVersion {
  number: number;
  parameters: JsonObject;
  universe: string[];
  cashInstrumentId: string | null;
  benchmarkInstrumentId: string;
  volatilityIndexInstrumentId: string | null;
  createdAt: string;
}

export interface Strategy {
  id: string;
  name: string;
  typeCode: string;
  status: 'Active' | 'Archived';
  createdAt: string;
  latestVersion: number;
  version: StrategyVersion;
}

export interface StrategyType {
  code: string;
  name: string;
  description: string;
  engine: string;
  engineVersion: string;
  supportsState: boolean;
  requiredSeriesRoles: string[];
  parametersSchema: Record<string, unknown>;
  defaultParameters: JsonObject;
  reports: ReportDefinition[];
}

export interface ReportDefinition {
  code: string;
  name: string;
  description: string;
  mode: 'rebalance' | 'riskReview';
  schedule: ReportTiming | null;
}

export interface StrategySetupInput {
  universe: string[];
  benchmarkInstrumentId: string;
  volatilityIndexInstrumentId: string | null;
  cashInstrumentId: string | null;
  parameters: JsonObject;
}
