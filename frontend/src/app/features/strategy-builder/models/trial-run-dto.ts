export interface TrialRunDto {
  number: number;
  strategyId: string;
  version: number;
  label: string | null;
  ranAt: string;
  status: 'Waiting' | 'Running' | 'Succeeded' | 'Failed';
  failureReason: string | null;
  annualReturn: number | null;
  annualReturnAfterTax: number | null;
  volatility: number | null;
  sharpe: number | null;
  maxDrawdown: number | null;
}
