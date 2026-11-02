export type BacktestStatus = 'Queued' | 'Running' | 'Succeeded' | 'Failed' | 'NotSupported';
export type ReplaySeriesKind = 'Strategy' | 'Benchmark' | 'EqualWeight';

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
}

export interface ReplaySeriesDto {
  kind: ReplaySeriesKind;
  summary: ReplaySummaryDto;
  points: ReplayPointDto[];
}

export interface ReplayResultDto {
  start: string;
  end: string;
  trades: number;
  riskExits: number;
  signalReports: number;
  series: ReplaySeriesDto[];
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

export interface MoneyOutcomesDto {
  contributed: number;
  years: number;
  strategy: MoneyOutcomesOfDto;
  benchmark: MoneyOutcomesOfDto;
}
