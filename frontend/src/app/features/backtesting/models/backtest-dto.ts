import { ApiSchema } from '../../../core/api/api-schema';

export type BacktestStatus =
  'Queued' | 'Running' | 'Succeeded' | 'Failed' | 'Cancelled' | 'NotSupported';
export const STRATEGY_SERIES = 'strategy';

export type ReplayPointDto = ApiSchema<'ReplayPointDto'>;
export type ReplaySummaryDto = ApiSchema<'ReplaySummaryDto'>;
export type ReplaySeriesDto = ApiSchema<'ReplaySeriesDto'>;
export type ReplayResultDto = ApiSchema<'ReplayResultDto'>;

export type BacktestDto = Omit<ApiSchema<'BacktestDto'>, 'status'> & { status: BacktestStatus };

export type MoneyOutcomeDto = ApiSchema<'MoneyOutcomeDto'>;
export type MoneyOutcomeRangeDto = ApiSchema<'MoneyOutcomeRangeDto'>;
export type SeriesMoneyOutcomesDto = ApiSchema<'SeriesMoneyOutcomesDto'>;
export type MoneyOutcomesDto = ApiSchema<'MoneyOutcomesDto'>;
