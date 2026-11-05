import { ReportTiming } from '../../reports/models/report-schedule';

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
  | 'RiskResult';

export type JsonValue =
  string | number | boolean | null | JsonValue[] | { [key: string]: JsonValue };

export interface NodeDto {
  id: string;
  kind: NodeKind;
  block: BlockKind | 'Shared';
  parameters: Record<string, JsonValue>;
}

export interface LinkDto {
  from: string;
  to: string;
  input: string;
}

export interface GraphDto {
  nodes: NodeDto[];
  links: LinkDto[];
}

export interface NodePosition {
  x: number;
  y: number;
}

export interface LayoutDto {
  positions: Record<string, NodePosition>;
  collapsed: BlockKind[];
}

export interface DraftSetupDto {
  funds: string[];
  benchmarkId: string | null;
  cashId: string | null;
  volatilityIndexId: string | null;
  extraSeries: string[];
}

export interface DraftReportDto {
  code: string;
  schedule: ReportTiming;
  signalKinds: string[];
  signalIds: string[];
}

export interface DraftProblemDto {
  message: string;
  severity: 'Error' | 'Warning';
  nodeId: string | null;
  block: BlockKind | null;
  reportCode: string | null;
}

export interface BlockWordsDto {
  block: BlockKind;
  sentence: string;
}

export interface DraftDto {
  id: string;
  name: string;
  description: string | null;
  revision: number;
  updatedAt: string;
  setup: DraftSetupDto;
  graph: GraphDto;
  layout: LayoutDto;
  reports: DraftReportDto[];
  originStrategyId: string | null;
  originVersion: number | null;
  problems: DraftProblemDto[];
  words: BlockWordsDto[];
}

export interface DraftChangeDto {
  revision: number;
  name?: string | null;
  description?: string | null;
  setup?: DraftSetupDto | null;
  graph?: GraphDto | null;
  reports?: DraftReportDto[] | null;
}

export interface DraftSummaryDto {
  id: string;
  name: string;
  originStrategyName: string | null;
  originVersion: number | null;
  updatedAt: string;
  lastTrialAt: string | null;
}

export interface DraftCheckDto {
  isClean: boolean;
  problems: DraftProblemDto[];
  signalIds: string[];
}

export interface DraftRulesDto {
  json: string;
}
