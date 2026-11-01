import {
  ActionKind,
  ActionStatus,
  FillDto,
  PlanReviewDto,
  ReportKind,
  ReportStatus,
  SectionColumnDto,
  SectionRiskDto,
} from './report-dto';

export interface ReportSummaryVm {
  code: string;
  id: string;
  accountId: string;
  title: string;
  periodLabel: string;
  kind: ReportKind;
  kindLabel: string;
  status: ReportStatus;
  triggerLabel: string;
  marketAsOf: string;
  generatedAt: string;
  closedAt: string | null;
  currency: string;
  contribution: number;
  actionsTotal: number;
  actionsOpen: number;
  supersededBy: string | null;
  accountName: string;
}

export interface ReportRequestVm {
  id: string;
  code: string;
  title: string;
  kindLabel: string;
  triggerLabel: string;
  status: 'Pending' | 'Failed';
  requestedAt: string;
  progressLabel: string | null;
  error: string | null;
}

export type ReportListRow =
  | { id: string; report: ReportSummaryVm; request?: never }
  | { id: string; request: ReportRequestVm; report?: never };

export interface AllocationPartVm {
  subPortfolio: string;
  quantity: number;
  reason: string | null;
}

export interface ReportActionVm {
  id: string;
  kind: ActionKind;
  title: string;
  instrumentName: string;
  instrumentId: string | null;
  symbol: string;
  reason: string;
  isMoney: boolean;
  plannedQuantity: number;
  plannedPrice: number;
  plannedAmount: number;
  currency: string;
  status: ActionStatus;
  filledQuantity: number;
  remainingQuantity: number;
  fillSummary: string | undefined;
  fills: FillDto[];
  awaitingJournal: boolean;
  skipReason: string | null;
  rejection: string | null;
  canMark: boolean;
  canPartiallyFill: boolean;
  canSkip: boolean;
  canUndo: boolean;
  allocations: AllocationPartVm[];
  taxLabel: string | null;
  hasBreakdown: boolean;
  isSell: boolean;
}

export interface ScoreTableVm {
  title: string;
  instrumentLabel: string;
  columns: SectionColumnDto[];
  rows: ScoreVm[];
}

export interface ScoreVm {
  symbol: string;
  name: string;
  group: string;
  rank: number | null;
  values: (number | boolean | string | null)[];
  status: string;
  reason: string;
  isTarget: boolean;
  targetWeight: number | null;
}

export interface SectionOrderVm {
  kindLabel: string;
  severity: 'success' | 'danger' | 'info' | 'secondary' | 'warn';
  symbol: string;
  quantity: number;
  price: number;
  amount: number;
  reason: string;
}

export interface SectionAllocationVm {
  label: string;
  role: 'Pick' | 'Exit' | 'Cash';
  valueBefore: number;
  valueAfter: number;
  weightBefore: number;
  weightAfter: number;
  targetWeight: number | null;
}

export interface SectionVm {
  id: string;
  name: string;
  targetShare: number;
  strategyLabel: string;
  contribution: number;
  marketIsFavorable: boolean | null;
  investFraction: number | null;
  marketReasons: string[];
  summary: string;
  warnings: string[];
  notes: string[];
  orders: SectionOrderVm[];
  allocation: SectionAllocationVm[];
  scores: ScoreTableVm | null;
  totalBefore: number;
  totalAfter: number;
  parkedBefore: number;
  parkedAfter: number;
  risk: (SectionRiskDto & { positionsVm: RiskPositionVm[] }) | null;
  error: string | null;
}

export interface RiskPositionVm {
  symbol: string;
  status: string;
  quantity: number;
  value: number;
  pnlPct: number;
  drawdownFromHigh: number;
  exitPrice: number | null;
  exitRule: string;
  exitDistance: number | null;
  alerts: string[];
  sell: boolean;
}

export interface ContributionVm {
  total: number;
  splitLabel: string;
  parts: { name: string; amount: number }[];
  transfer: { from: string; to: string; amount: number } | null;
}

export interface TaxImpactVm {
  change: string;
  yearChange: string | null;
  carriedLoss: string | null;
  rowsNote: string | null;
  notes: string[];
}

export interface ReportVm {
  summary: ReportSummaryVm;
  isOpenForMarking: boolean;
  actions: ReportActionVm[];
  sections: SectionVm[];
  warnings: string[];
  quotesLookStale: boolean;
  contribution: ContributionVm;
  review: PlanReviewDto | null;
  taxImpact: TaxImpactVm | null;
  totals: { buys: number; sells: number };
}

export interface ReportProgressVm {
  total: number;
  resolved: number;
  executed: number;
  partial: number;
  skipped: number;
  percent: number;
}

export interface FillInput {
  quantity: number;
  price: number;
  fee: number;
  executedOn: string;
  note: string | null;
}
