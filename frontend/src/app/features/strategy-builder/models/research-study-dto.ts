import { ReplaySummaryDto } from '../../backtesting/models/backtest-dto';

export interface ResearchStudyDto {
  kind: 'sensitivity' | 'out-of-sample' | 'walk-forward';
  window: {
    training: { start: string; end: string } | null;
    test: { start: string; end: string };
  };
  parameterValue: number | null;
  summary: ReplaySummaryDto | null;
  trainingSharpe: number | null;
  failureReason: string | null;
}
