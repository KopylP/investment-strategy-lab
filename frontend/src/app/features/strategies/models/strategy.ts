import { JsonObject } from '../../../shared/ui/schema-form/schema-form';

export interface StrategyVersion {
  number: number;
  parameters: JsonObject;
  universe: string[];
  cashInstrumentId: string | null;
  benchmarkInstrumentId: string;
  volatilityIndexInstrumentId: string | null;
  rebalanceFrequency: string;
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
}

export interface StrategySetupInput {
  universe: string[];
  benchmarkInstrumentId: string;
  volatilityIndexInstrumentId: string | null;
  cashInstrumentId: string | null;
  rebalanceFrequency: string;
  parameters: JsonObject;
}
