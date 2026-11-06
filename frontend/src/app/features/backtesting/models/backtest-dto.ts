export type BacktestStatus = 'Queued' | 'Running' | 'Succeeded' | 'Failed' | 'NotSupported';
export const STRATEGY_SERIES = 'strategy';

export interface ReplayPointDto {
  date: string;
  growth: number;
  worstDrawdown: number;
}

export interface ReplaySummaryDto {
  totalReturn: number;
  annualReturn: number;
  volatility: number;
  sharpe: number;
  maxDrawdown: number;
  annualReturnAfterTax: number | null;
}

export interface ReplaySeriesDto {
  code: string;
  label: string;
  summary: ReplaySummaryDto;
  points: ReplayPointDto[];
}

export interface ReplayResultDto {
  start: string;
  end: string;
  currency: string;
  trades: number;
  riskExits: number;
  signalReports: number;
  series: ReplaySeriesDto[];
  initialCapital?: number;
  finalCapital?: number;
}

export interface BacktestDto {
  strategyId: string;
  strategyVersion: number;
  status: BacktestStatus;
  requestedAt: string;
  finishedAt: string | null;
  failureReason: string | null;
  result: ReplayResultDto | null;
}

export interface MoneyOutcomeDto {
  final: number;
  from: string;
  to: string;
}

export interface MoneyOutcomesOfDto {
  periods: number;
  worst: MoneyOutcomeDto | null;
  typical: MoneyOutcomeDto | null;
  best: MoneyOutcomeDto | null;
}

export interface SeriesMoneyOutcomesDto {
  code: string;
  label: string;
  outcomes: MoneyOutcomesOfDto;
}

export interface MoneyOutcomesDto {
  contributed: number;
  years: number;
  series: SeriesMoneyOutcomesDto[];
}
