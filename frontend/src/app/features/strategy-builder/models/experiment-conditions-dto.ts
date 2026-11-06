export interface ExperimentConditionsDto {
  period: { start: string; end: string };
  initialCapital: number;
  costs: {
    commissionRate: number;
    feePerTrade: number;
    spreadBps: number;
    slippageBps: number;
  };
  robustness: {
    outOfSampleStart: string | null;
    walkForward: { trainingMonths: number; testMonths: number } | null;
    neighborhood: { path: string; values: number[] } | null;
  };
}
