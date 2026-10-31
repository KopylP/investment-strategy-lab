/** Report as the API returns it (Rebalancing module). */

export type ReportStatus = 'New' | 'InProgress' | 'Closed' | 'Superseded';
export type ReportKind = 'Monthly' | 'Recalculation' | 'RiskReview';
export type ActionKind = 'Contribute' | 'Sell' | 'Unpark' | 'Transfer' | 'Buy' | 'Park';
export type ActionStatus = 'Pending' | 'PartiallyExecuted' | 'Executed' | 'Skipped';

export interface ReportSummaryDto {
  id: string;
  accountId: string;
  accountName: string;
  period: string;
  kind: ReportKind;
  /** Which of the strategies' reports this is, e.g. "rebalance". */
  code: string;
  /** Its name as the strategy declared it. */
  title: string;
  trigger: 'Scheduled' | 'OnDemand' | 'Signal';
  status: ReportStatus;
  marketAsOf: string;
  generatedAt: string;
  closedAt: string | null;
  currency: string;
  contribution: number;
  actionsTotal: number;
  actionsOpen: number;
  supersededBy: string | null;
}

/** A report asked for: «Готується» while its strategies think (minutes or hours for an AI agent), then made or failed. */
export interface ReportRequestDto {
  id: string;
  accountId: string;
  code: string;
  title: string;
  kind: ReportKind;
  trigger: 'Scheduled' | 'OnDemand' | 'Signal';
  status: 'Pending' | 'Completed' | 'Failed';
  /** The market date the report is for (yyyy-MM-dd): its period. */
  asOf: string;
  requestedAt: string;
  /** Strategies still silent by then count as not answered. */
  deadline: string;
  completedAt: string | null;
  reportId: string | null;
  error: string | null;
  strategies: number;
  strategiesAnswered: number;
}

export interface FillDto {
  number: number;
  quantity: number;
  price: number;
  fee: number;
  executedOn: string;
  note: string | null;
  transactionId: string | null;
  /** Marked with an entry that was already in the journal (e.g. an imported statement row). */
  linked?: boolean;
}

export interface ActionAllocationDto {
  subPortfolioId: string;
  quantity: number;
  reason: string | null;
}

export interface ReportActionDto {
  id: string;
  sequence: number;
  kind: ActionKind;
  instrumentId: string | null;
  plannedQuantity: number;
  plannedPrice: number;
  plannedAmount: number;
  allocations: ActionAllocationDto[];
  fromSubPortfolioId: string | null;
  toSubPortfolioId: string | null;
  reason: string;
  status: ActionStatus;
  filledQuantity: number;
  remainingQuantity: number;
  fills: FillDto[];
  skipReason: string | null;
  rejection: string | null;
  estimatedGainPln: number | null;
  estimatedTaxPln: number | null;
}

export interface SectionScoreDto {
  instrumentId: string;
  name: string;
  group: string;
  price: number | null;
  momentum: number | null;
  return1m: number | null;
  return3m: number | null;
  return12m: number | null;
  volatility: number | null;
  aboveMovingAverage: boolean | null;
  rank: number | null;
  status: string;
  reason: string;
  /** The strategy's metrics under the ids of SectionDecisionDto.columns; absent in older reports. */
  values?: Record<string, number | boolean | string | null> | null;
}

export type ColumnFormat = 'percent' | 'signedPercent' | 'number' | 'money' | 'trend' | 'text';

/** A column of the "why" table as the strategy described it. */
export interface SectionColumnDto {
  id: string;
  label: string;
  format: ColumnFormat;
  hint: string | null;
  /** Shown on a phone too. */
  compact: boolean;
  trueHint: string | null;
  falseHint: string | null;
}

export interface SectionDecisionDto {
  marketIsFavorable: boolean;
  investFraction: number;
  marketReasons: string[];
  summary: string;
  targets: { instrumentId: string; weight: number }[];
  scores: SectionScoreDto[];
  warnings: string[];
  /** Null in reports made before strategies described their table. */
  scoreTitle?: string | null;
  instrumentLabel?: string | null;
  columns?: SectionColumnDto[] | null;
}

export interface SectionOrderDto {
  kind: 'Exit' | 'Trim' | 'Unpark' | 'Buy' | 'Park';
  instrumentId: string;
  quantity: number;
  price: number;
  amount: number;
  reason: string;
}

export interface SectionAllocationDto {
  instrumentId: string | null;
  role: 'Pick' | 'Exit' | 'Cash';
  valueBefore: number;
  valueAfter: number;
  weightBefore: number;
  weightAfter: number;
  targetWeight: number | null;
}

export interface SectionRiskPositionDto {
  instrumentId: string;
  status: string;
  quantity: number;
  averageCost: number;
  currentPrice: number;
  value: number;
  pnlPct: number;
  drawdownFromHigh: number;
  exitPrice: number | null;
  exitRule: string;
  exitDistance: number | null;
  alerts: string[];
  sell: boolean;
}

export interface SectionRiskDto {
  status: 'ok' | 'watch' | 'act';
  marketAlerts: string[];
  positions: SectionRiskPositionDto[];
  portfolioAlerts: string[];
  actions: string[];
}

export interface ReportSectionDto {
  subPortfolioId: string;
  name: string;
  targetShare: number;
  strategyId: string | null;
  strategyVersion: number | null;
  strategyCode: string | null;
  engineVersion: string | null;
  contribution: number;
  decision: SectionDecisionDto | null;
  orders: SectionOrderDto[];
  allocation: SectionAllocationDto[];
  totalBefore: number;
  totalAfter: number;
  parkedBefore: number;
  parkedAfter: number;
  notes: string[];
  risk: SectionRiskDto | null;
  error: string | null;
}

export interface ContributionPlanDto {
  total: number;
  split: string;
  parts: { subPortfolioId: string; amount: number }[];
  transfer: { fromSubPortfolioId: string; toSubPortfolioId: string; amount: number } | null;
}

export interface PlanReviewDto {
  reviewer: string;
  verdict: string;
  summary: string;
  findings: { severity: string; message: string }[];
}

export interface ReportDetailsDto {
  summary: ReportSummaryDto;
  contributionPlan: ContributionPlanDto;
  actions: ReportActionDto[];
  sections: ReportSectionDto[];
  warnings: string[];
  review: PlanReviewDto | null;
  taxEstimate: TaxEstimateDto | null;
  instruments: Record<string, { symbol: string; name: string; isin: string | null; currency: string }>;
  subPortfolios: Record<string, string>;
}

export interface PlanSettingsDto {
  accountId: string;
  monthlyContribution: number;
  minTrade: number;
  trimBand: number;
  allowFractionalShares: boolean;
  generateOnSchedule: boolean;
}

export interface TaxEstimateDto {
  policyCode: string;
  isTaxed: boolean;
  gainPln: number;
  taxPln: number;
  fxRate: number | null;
  notes: string[];
}

export interface MatchedOperationDto {
  transactionId: string;
  occurredOn: string;
  kind: 'Buy' | 'Sell' | 'Contribution';
  instrumentId: string | null;
  quantity: number;
  price: number;
  fee: number;
  source: string;
}

export interface ActionMatchDto {
  actionId: string;
  sequence: number;
  actionKind: string;
  instrumentId: string | null;
  plannedQuantity: number;
  remainingQuantity: number;
  completes: boolean;
  operations: MatchedOperationDto[];
}

export interface LoadOperationsPreviewDto {
  matches: ActionMatchDto[];
  unmatched: MatchedOperationDto[];
}
