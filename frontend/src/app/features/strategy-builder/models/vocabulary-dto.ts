import { ApiSchema } from '../../../core/api/api-schema';
import { JsonValue, NodeKind } from './draft-dto';

export type SocketType =
  'Number' | 'Boolean' | 'Funds' | 'Series' | 'SeriesList' | 'Candidates' | 'Budget' | 'RiskPolicy';

export type VocabularyFieldDto = Omit<ApiSchema<'VocabularyFieldDto'>, 'default'> & {
  default: JsonValue | null;
};

export type IndicatorKindDto = Omit<ApiSchema<'IndicatorKindDto'>, 'fields'> & {
  fields: VocabularyFieldDto[];
};

export type ConditionKindDto = Omit<ApiSchema<'ConditionKindDto'>, 'fields'> & {
  fields: VocabularyFieldDto[];
};

export type InputSocketDto = Omit<ApiSchema<'InputSocketDto'>, 'type'> & {
  type: SocketType;
};

export type NodeKindDto = Omit<ApiSchema<'NodeKindDto'>, 'kind' | 'inputs' | 'output'> & {
  kind: NodeKind;
  inputs: InputSocketDto[];
  output: SocketType | null;
};

export type VocabularyDto = Omit<
  ApiSchema<'VocabularyDto'>,
  'indicators' | 'conditions' | 'blockFields' | 'nodeKinds'
> & {
  indicators: IndicatorKindDto[];
  conditions: ConditionKindDto[];
  blockFields: Record<string, VocabularyFieldDto>;
  nodeKinds: NodeKindDto[];
};

export type ReportKindDto = ApiSchema<'ReportKindDto'>;
export type TemplateDto = ApiSchema<'TemplateDto'>;
