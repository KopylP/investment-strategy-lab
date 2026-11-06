import { JsonValue, NodeKind } from './draft-dto';

export type FieldKind =
  'Integer' | 'Number' | 'Fraction' | 'Series' | 'Choice' | 'Indicator' | 'Signal';
export type SocketType =
  'Number' | 'Boolean' | 'Funds' | 'Series' | 'SeriesList' | 'Candidates' | 'Budget' | 'RiskPolicy';

export interface VocabularyFieldDto {
  name: string;
  kind: FieldKind;
  label: string;
  minimum: number | null;
  maximum: number | null;
  required: boolean;
  default: JsonValue | null;
  choiceLabels: Record<string, string>;
}

export interface IndicatorKindDto {
  code: string;
  label: string;
  hint: string;
  phrase: string;
  columnFormat: string;
  fields: VocabularyFieldDto[];
}

export interface ConditionKindDto {
  code: string;
  label: string;
  hint: string;
  phrase: string;
  fields: VocabularyFieldDto[];
}

export interface InputSocketDto {
  name: string;
  type: SocketType;
  required: boolean;
  many: boolean;
}

export interface NodeKindDto {
  kind: NodeKind;
  inputs: InputSocketDto[];
  output: SocketType | null;
}

export interface ReportKindDto {
  code: string;
  name: string;
  description: string;
  signalKinds: string[];
}

export interface TemplateDto {
  code: string;
  name: string;
  description: string;
}

export interface VocabularyDto {
  indicators: IndicatorKindDto[];
  conditions: ConditionKindDto[];
  blockFields: Record<string, VocabularyFieldDto>;
  nodeKinds: NodeKindDto[];
  reports: ReportKindDto[];
  templates: TemplateDto[];
}
