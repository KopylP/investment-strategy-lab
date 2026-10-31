
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
  { label: 'Щогодини', value: 'hourly' },
  { label: 'Щодня', value: 'daily' },
  { label: 'Щотижня', value: 'weekly' },
  { label: 'Щомісяця', value: 'monthly' },
  { label: 'Щокварталу', value: 'quarterly' },
  { label: 'Лише вручну', value: 'off' },
];

const weekdayLabels: Record<string, string> = {
  monday: 'понеділок',
  tuesday: 'вівторок',
  wednesday: 'середа',
  thursday: 'четвер',
  friday: 'пʼятниця',
  saturday: 'субота',
  sunday: 'неділя',
};

export const weekdayOptions = Object.entries(weekdayLabels).map(([value, label]) => ({ label: label[0].toUpperCase() + label.slice(1), value }));

export function scheduleLabel(timing: ReportTiming): string {
  const at = `о ${timing.time}`;
  switch (timing.frequency.toLowerCase()) {
    case 'hourly':
      return `щогодини о :${String(timing.minute).padStart(2, '0')}`;
    case 'daily':
      return `щодня ${at}`;
    case 'weekly':
      return `щотижня, ${weekdayLabels[timing.weekday.toLowerCase()] ?? timing.weekday} ${at}`;
    case 'monthly':
      return `щомісяця, ${timing.day}-го ${at}`;
    case 'quarterly':
      return `щокварталу, ${timing.day}-го першого місяця ${at}`;
    default:
      return 'лише вручну';
  }
}

export function editableTiming({ frequency, day, weekday, time, minute }: ReportTiming): ReportTiming {
  return { frequency: frequency.toLowerCase(), day, weekday: weekday.toLowerCase(), time, minute };
}

export function isTimingComplete(timing: ReportTiming): boolean {
  switch (timing.frequency) {
    case 'hourly':
      return Number.isInteger(timing.minute) && timing.minute >= 0 && timing.minute <= 59;
    case 'off':
      return true;
    default:
      return /^([01]\d|2[0-3]):[0-5]\d$/.test(timing.time ?? '')
        && (!['monthly', 'quarterly'].includes(timing.frequency) || (Number.isInteger(timing.day) && timing.day >= 1 && timing.day <= 31));
  }
}

export function distinctReports(reports: ScheduledReport[]): ScheduledReport[] {
  return reports.filter((report, index) => reports.findIndex((other) => other.code === report.code) === index);
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
    if (!known || (report.next && (!known.next || report.next < known.next))) soonest.set(report.code, report);
  }
  return [...soonest.values()].sort((a, b) => (a.next ?? '￿').localeCompare(b.next ?? '￿'));
}
