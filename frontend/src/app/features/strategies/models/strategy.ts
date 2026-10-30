import { JsonObject } from '../../../shared/ui/schema-form/schema-form';

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

/** A report the type makes; a strategy's parameters may set how often (frequencyParameter). */
export interface ReportDefinition {
  code: string;
  name: string;
  description: string;
  mode: 'rebalance' | 'riskReview';
  /** off, weekly, monthly or quarterly: the type's default. */
  frequency: string;
  time: string;
  /** Dotted camelCase path in the parameters, e.g. "reporting.rebalance". */
  frequencyParameter: string | null;
}

/** How often for a strategy with these parameters: its value at the report's frequency parameter, else the default. */
export function frequencyOf(report: ReportDefinition, parameters: JsonObject): string {
  let node: unknown = parameters;
  for (const key of report.frequencyParameter?.split('.') ?? []) node = node && typeof node === 'object' ? (node as Record<string, unknown>)[key] : undefined;
  return report.frequencyParameter && typeof node === 'string' ? node : report.frequency;
}

export interface StrategySetupInput {
  universe: string[];
  benchmarkInstrumentId: string;
  volatilityIndexInstrumentId: string | null;
  cashInstrumentId: string | null;
  parameters: JsonObject;
}
