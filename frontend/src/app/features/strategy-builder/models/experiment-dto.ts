import { ApiSchema } from '../../../core/api/api-schema';
import { BacktestStatus } from '../../backtesting/models/backtest-dto';
import { ResearchReportDto } from './research-report-dto';

export type ExperimentDto = Omit<ApiSchema<'ExperimentDto'>, 'status' | 'result'> & {
  status: BacktestStatus;
  result: ResearchReportDto | null;
};
