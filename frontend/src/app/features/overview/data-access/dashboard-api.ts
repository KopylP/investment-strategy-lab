export interface DashboardDto {
  /** Null for all accounts together. */
  accountId: string | null;
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
  series: { date: string; value: number; netContributions: number; twrIndex: number; planValue: number | null; planNetContributions: number | null; drawdown: number }[];
  subPortfolios: { id: string; name: string; value: number; targetShare: number; actualShare: number; twr: number | null; netContributions: number }[];
  /** All-accounts dashboard only: values in the dashboard currency, TWR in the account's own. */
  accounts: { id: string; name: string; broker: string; baseCurrency: string; value: number; share: number; netContributions: number; twr: number | null }[];
  reports: { reportId: string; date: string; kind: string; period: string; accountId: string }[];
}
