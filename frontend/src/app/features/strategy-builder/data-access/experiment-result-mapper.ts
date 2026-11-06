import { BacktestDto } from '../../backtesting/models/backtest-dto';
import { ExperimentDto } from '../models/experiment-dto';

export function experimentBacktest(experiment: ExperimentDto): BacktestDto {
  return {
    strategyId: experiment.strategyId,
    strategyVersion: experiment.strategyVersion,
    status: experiment.status,
    requestedAt: experiment.requestedAt,
    finishedAt: experiment.finishedAt,
    failureReason: experiment.failureReason,
    result: experiment.result?.baseline ?? null,
  };
}
