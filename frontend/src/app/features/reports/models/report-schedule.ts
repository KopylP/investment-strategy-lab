import { translate } from '@jsverse/transloco';
export type ReportMode = 'rebalance' | 'riskReview';

export interface ReportTiming {
  frequency: string;
  day: number;
  weekday: string;
  time: string;
  minute: number;
}

export interface ScheduledReport {
  code: string;
  name: string;
  description: string;
  mode: ReportMode;
  schedule: ReportTiming;
  strategySchedule: ReportTiming;
  own: boolean;
  next: string | null;
  strategies: string[];
}

export interface ReportSchedule {
  generateOnSchedule: boolean;
  timeZone: string;
  reports: ScheduledReport[];
}

export const frequencyOptions = [
  { label: translate('signals.editor.frequency.hourly'), value: 'hourly' },
  { label: translate('signals.editor.frequency.daily'), value: 'daily' },
  { label: translate('signals.editor.frequency.weekly'), value: 'weekly' },
  { label: translate('reports.schedule.frequency.monthly'), value: 'monthly' },
  { label: translate('reports.schedule.frequency.quarterly'), value: 'quarterly' },
  { label: translate('reports.schedule.off'), value: 'off' },
];

const weekdayLabels: Record<string, string> = {
  monday: translate('weekday.1'),
  tuesday: translate('weekday.2'),
  wednesday: translate('weekday.3'),
  thursday: translate('weekday.4'),
  friday: translate('weekday.5'),
  saturday: translate('weekday.6'),
  sunday: translate('weekday.0'),
};

export const weekdayOptions = Object.entries(weekdayLabels).map(([value, label]) => ({
  label: label[0].toUpperCase() + label.slice(1),
  value,
}));

export function scheduleLabel(timing: ReportTiming): string {
  const time = timing.time;
  switch (timing.frequency.toLowerCase()) {
    case 'hourly':
      return translate('reports.schedule.label.hourly', {
        minute: String(timing.minute).padStart(2, '0'),
      });
    case 'daily':
      return translate('reports.schedule.label.daily', { time });
    case 'weekly':
      return translate('reports.schedule.label.weekly', {
        weekday: weekdayLabels[timing.weekday.toLowerCase()] ?? timing.weekday,
        time,
      });
    case 'monthly':
      return translate('reports.schedule.label.monthly', { day: timing.day, time });
    case 'quarterly':
      return translate('reports.schedule.label.quarterly', { day: timing.day, time });
    default:
      return translate('reports.schedule.manualLower');
  }
}

export function editableTiming({
  frequency,
  day,
  weekday,
  time,
  minute,
}: ReportTiming): ReportTiming {
  return { frequency: frequency.toLowerCase(), day, weekday: weekday.toLowerCase(), time, minute };
}

export function isTimingComplete(timing: ReportTiming): boolean {
  switch (timing.frequency) {
    case 'hourly':
      return Number.isInteger(timing.minute) && timing.minute >= 0 && timing.minute <= 59;
    case 'off':
      return true;
    default:
      return (
        /^([01]\d|2[0-3]):[0-5]\d$/.test(timing.time ?? '') &&
        (!['monthly', 'quarterly'].includes(timing.frequency) ||
          (Number.isInteger(timing.day) && timing.day >= 1 && timing.day <= 31))
      );
  }
}

export function distinctReports(reports: ScheduledReport[]): ScheduledReport[] {
  return reports.filter(
    (report, index) => reports.findIndex((other) => other.code === report.code) === index,
  );
}

export function reportIcon(mode: ReportMode): string {
  return mode === 'riskReview' ? 'pi pi-shield' : 'pi pi-bolt';
}

export function nextLabel(next: string): string {
  const [date, time] = next.split('T');
  const [year, month, day] = date.split('-');

  return `${day}.${month}.${year}, ${time.slice(0, 5)}`;
}

export function upcomingReports(reports: ScheduledReport[]): ScheduledReport[] {
  const soonest = new Map<string, ScheduledReport>();

  for (const report of reports) {
    const known = soonest.get(report.code);
    if (!known || (report.next && (!known.next || report.next < known.next)))
      soonest.set(report.code, report);
  }

  return [...soonest.values()].sort((a, b) => (a.next ?? '￿').localeCompare(b.next ?? '￿'));
}
