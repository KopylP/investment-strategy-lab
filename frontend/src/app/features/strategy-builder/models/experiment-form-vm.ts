export interface ExperimentFormVm {
  label: string;
  start: string;
  end: string;
  initialCapital: number;
  commissionPercent: number;
  feePerTrade: number;
  spreadBps: number;
  slippageBps: number;
  independent: boolean;
  outOfSampleStart: string;
  walkForward: boolean;
  trainingMonths: number;
  testMonths: number;
  sensitivity: boolean;
  parameterPath: string | null;
  parameterValues: string;
}
