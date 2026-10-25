export interface DashboardDto {
  accountId: string;
  currency: string;
  asOf: string | null;
  period: string;
  value: number;
  netContributions: number;
  gain: number;
  twr: number | null;
  twrAnnualized: number | null;
  xirr: number | null;
  maxDrawdown: number;
  currentDrawdown: number;
  planValue: number | null;
  taxDueThisYearPln: number | null;
  series: { date: string; value: number; netContributions: number; twrIndex: number; planValue: number | null; drawdown: number }[];
  subPortfolios: { id: string; name: string; value: number; targetShare: number; actualShare: number; twr: number | null; netContributions: number }[];
  reports: { reportId: string; date: string; kind: string; period: string }[];
}
