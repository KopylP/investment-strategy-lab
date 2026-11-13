import { ApiSchema } from '../../../core/api/api-schema';

export type BlockKind = 'MarketFilter' | 'Exit' | 'Entry' | 'Selection' | 'Risk';

export type NodeKind =
  | 'Indicator'
  | 'Compare'
  | 'Between'
  | 'Crossed'
  | 'Signal'
  | 'All'
  | 'Any'
  | 'Not'
  | 'MarketResult'
  | 'ExitResult'
  | 'EntryResult'
  | 'SelectionResult'
  | 'RiskResult'
  | 'Setup'
  | 'FundSelection'
  | 'Cash'
  | 'Portfolio';

export type JsonValue =
  string | number | boolean | null | JsonValue[] | { [key: string]: JsonValue };

export type NodeDto = Omit<ApiSchema<'NodeDto'>, 'kind' | 'block' | 'parameters'> & {
  kind: NodeKind;
  block: BlockKind | 'Shared';
  parameters: Record<string, JsonValue>;
};

export type LinkDto = ApiSchema<'LinkDto'>;

export type GraphDto = Omit<ApiSchema<'GraphDto'>, 'nodes'> & {
  nodes: NodeDto[];
};

export type NodePosition = ApiSchema<'NodePositionDto'>;

export type LayoutDto = Omit<ApiSchema<'LayoutDto'>, 'collapsed'> & {
  collapsed: BlockKind[];
};

export type DraftSetupDto = ApiSchema<'DraftSetupDto'>;
export type DraftReportDto = ApiSchema<'DraftReportDto'>;

export type DraftProblemDto = Omit<ApiSchema<'DraftProblemDto'>, 'severity' | 'block'> & {
  severity: 'Error' | 'Warning';
  block: BlockKind | null;
};

export type BlockWordsDto = Omit<ApiSchema<'BlockWordsDto'>, 'block'> & {
  block: BlockKind;
};

export type DraftDto = Omit<ApiSchema<'DraftDto'>, 'graph' | 'layout' | 'problems' | 'words'> & {
  graph: GraphDto;
  layout: LayoutDto;
  problems: DraftProblemDto[];
  words: BlockWordsDto[];
};

export type DraftChangeDto = Omit<ApiSchema<'DraftChangeDto'>, 'graph'> & {
  graph: GraphDto | null;
};

export type DraftSummaryDto = ApiSchema<'DraftSummaryDto'>;

export type DraftCheckDto = Omit<ApiSchema<'DraftCheckDto'>, 'problems'> & {
  problems: DraftProblemDto[];
};

export type DraftRulesDto = ApiSchema<'DraftRulesDto'>;
export type CreateDraftBody = ApiSchema<'CreateDraftCommand'>;
export type ImportRulesBody = ApiSchema<'ImportRulesCommand'>;
export type CopyEntryToExitBody = ApiSchema<'CopyEntryToExitCommand'>;
