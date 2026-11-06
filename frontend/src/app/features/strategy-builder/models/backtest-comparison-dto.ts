export interface BacktestComparisonDto {
  comparable: boolean;
  reason: string | null;
  difference: {
    annualReturn: number;
    annualReturnAfterTax: number | null;
    volatility: number;
    sharpe: number;
    maxDrawdown: number;
  } | null;
}
