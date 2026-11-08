import { translate } from '@jsverse/transloco';
import { TrialRunDto } from './trial-run-dto';

export interface TrialStatusVm {
  label: string;
  tone: 'neutral' | 'progress' | 'success' | 'danger';
}

export const trialStatuses: Record<TrialRunDto['status'], TrialStatusVm> = {
  Waiting: { label: translate('builder.trialStatus.Pending'), tone: 'neutral' },
  Running: { label: translate('builder.trialStatus.Running'), tone: 'progress' },
  Succeeded: { label: translate('builder.trialStatus.Finished'), tone: 'success' },
  Failed: { label: translate('builder.trialStatus.Failed'), tone: 'danger' },
  Cancelled: { label: translate('builder.trialStatus.Cancelled'), tone: 'neutral' },
};
