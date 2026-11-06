import { BacktestStatus } from '../../backtesting/models/backtest-dto';
import { ExperimentConditionsDto } from './experiment-conditions-dto';
import { ResearchReportDto } from './research-report-dto';

export interface ExperimentDto {
  id: string;
  strategyId: string;
  strategyVersion: number;
  status: BacktestStatus;
  requestedAt: string;
  finishedAt: string | null;
  failureReason: string | null;
  conditions: ExperimentConditionsDto;
  result: ResearchReportDto | null;
}
