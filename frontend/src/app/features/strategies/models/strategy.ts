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

/** A report the type makes; a strategy's parameters may set when (scheduleParameter). */
export interface ReportDefinition {
  code: string;
  name: string;
  description: string;
  mode: 'rebalance' | 'riskReview';
  /** The type's default; null in a catalog synced before schedules existed. */
  schedule: ReportTiming | null;
  /** Dotted camelCase path in the parameters, e.g. "reporting.rebalance". */
  scheduleParameter: string | null;
}

/** When for a strategy with these parameters: its value at the report's schedule parameter over the default. */
export function scheduleOf(report: ReportDefinition, parameters: JsonObject): ReportTiming {
  const fallback = report.schedule ?? { frequency: 'off', day: 1, weekday: 'sunday', time: '20:00', minute: 0 };
  let node: unknown = parameters;
  for (const key of report.scheduleParameter?.split('.') ?? []) node = node && typeof node === 'object' ? (node as Record<string, unknown>)[key] : undefined;
  return report.scheduleParameter && node && typeof node === 'object' ? { ...fallback, ...(node as Partial<ReportTiming>) } : fallback;
}

export interface StrategySetupInput {
  universe: string[];
  benchmarkInstrumentId: string;
  volatilityIndexInstrumentId: string | null;
  cashInstrumentId: string | null;
  parameters: JsonObject;
}
