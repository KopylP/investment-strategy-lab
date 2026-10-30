/** The reports an account gets: what the strategies of its sub-portfolios declare (GET …/report-schedule). */

export type ReportMode = 'rebalance' | 'riskReview';
export type ReportFrequency = 'Off' | 'Weekly' | 'Monthly' | 'Quarterly';

export interface ScheduledReport {
  code: string;
  name: string;
  description: string;
  mode: ReportMode;
  frequency: ReportFrequency;
  /** HH:mm on the report's Sunday. */
  time: string;
  /** Null when off: made only on demand. */
  next: string | null;
  /** "Sub-portfolio: strategy" pairs it covers. */
  strategies: string[];
}

export interface ReportSchedule {
  generateOnSchedule: boolean;
  timeZone: string;
  reports: ScheduledReport[];
}

const frequencyLabels: Record<string, string> = {
  off: 'лише вручну',
  weekly: 'щотижня, неділя',
  monthly: 'щомісяця, перша неділя',
  quarterly: 'щокварталу, перша неділя кварталу',
};

/** "щомісяця, перша неділя" for Monthly or monthly. */
export function frequencyLabel(frequency: string): string {
  return frequencyLabels[frequency.toLowerCase()] ?? frequency;
}

/** One entry per report the account's strategies declare (strategies of one type may set different frequencies). */
export function distinctReports(reports: ScheduledReport[]): ScheduledReport[] {
  return reports.filter((report, index) => reports.findIndex((other) => other.code === report.code) === index);
}

export function reportIcon(mode: ReportMode): string {
  return mode === 'riskReview' ? 'pi pi-shield' : 'pi pi-bolt';
}
