import { ApiSchema } from '../../../core/api/api-schema';

export type TrialStatus = 'Waiting' | 'Running' | 'Succeeded' | 'Failed' | 'Cancelled';

export type TrialRunDto = Omit<ApiSchema<'TrialRunDto'>, 'status'> & {
  status: TrialStatus;
};

export type RunTrialBody = ApiSchema<'RunTrialRequest'>;
