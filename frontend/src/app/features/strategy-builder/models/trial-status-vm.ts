import { TrialRunDto } from './trial-run-dto';

export interface TrialStatusVm {
  label: string;
  tone: 'neutral' | 'progress' | 'success' | 'danger';
}

export const trialStatuses: Record<TrialRunDto['status'], TrialStatusVm> = {
  Waiting: { label: 'Очікує', tone: 'neutral' },
  Running: { label: 'Обчислюється', tone: 'progress' },
  Succeeded: { label: 'Готово', tone: 'success' },
  Failed: { label: 'Помилка', tone: 'danger' },
  Cancelled: { label: 'Скасовано', tone: 'neutral' },
};
