/** The reports an account gets: what the strategies of its sub-portfolios declare (GET …/report-schedule). */

export type ReportMode = 'rebalance' | 'riskReview';

/** When a report comes, in the service's time zone; each frequency reads only its own fields (any case). */
export interface ReportTiming {
  frequency: string;
  /** Monthly: day of the month; quarterly: of the quarter's first month (a shorter month: its last day). */
  day: number;
  /** Weekly: monday … sunday. */
  weekday: string;
  /** Daily, weekly, monthly, quarterly: HH:mm. */
  time: string;
  /** Hourly: the minute of every hour. */
  minute: number;
}

export interface ScheduledReport {
  code: string;
  name: string;
  description: string;
  mode: ReportMode;
  /** When it comes: the user's own time if `own`, else the strategy's. */
  schedule: ReportTiming;
  /** When its strategy says it comes. */
  strategySchedule: ReportTiming;
  /** The user set the time for the account (PUT …/report-schedule/{code}; DELETE — back to the strategy's). */
  own: boolean;
  /** Local yyyy-MM-ddTHH:mm of the next one; null when off: made only on demand. */
  next: string | null;
  /** "Sub-portfolio: strategy" pairs it covers. */
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

/** "щомісяця, 15-го о 20:00", "щотижня, пʼятниця о 18:00", "щогодини о :05", "лише вручну". */
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

/** One entry per report the account's strategies declare (strategies may declare different times). */
export function distinctReports(reports: ScheduledReport[]): ScheduledReport[] {
  return reports.filter((report, index) => reports.findIndex((other) => other.code === report.code) === index);
}

export function reportIcon(mode: ReportMode): string {
  return mode === 'riskReview' ? 'pi pi-shield' : 'pi pi-bolt';
}
