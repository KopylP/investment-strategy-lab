
export interface SubPortfolioSetup {
  id: string;
  name: string;
  targetShare: number;
  status: 'Active' | 'Retired';
  strategyId: string | null;
  pinnedStrategyVersion: number | null;
  cashInstrumentId: string | null;
  maxPositionWeight: number | null;
  maxMonthlyTurnover: number | null;
}

export interface AccountSetup {
  id: string;
  ownerId: string;
  name: string;
  broker: string;
  baseCurrency: string;
  taxPolicyCode: string;
  contributionPolicy: 'FillUnderweight' | 'FixedSplit';
  driftBand: number;
  subPortfolios: SubPortfolioSetup[];
}

export interface Instrument {
  id: string;
  kind: string;
  isin: string | null;
  name: string;
  exchange: string;
  tradingCurrency: string;
  group: string | null;
  isAccumulating: boolean | null;
  providerSymbols: string[];
  description: string | null;
}

export interface Lot {
  openedBy: string;
  openedOn: string;
  quantity: number;
  unitCost: number;
}

export interface Position {
  instrumentId: string;
  quantity: number;
  costBasis: number;
  averageCost: number;
  firstOpenedOn: string;
  lots: Lot[];
}

export interface SubPortfolioHoldings {
  subPortfolioId: string;
  name: string;
  cash: number;
  positions: Position[];
  foreignCash?: { currency: string; amount: number }[];
}

export interface AccountHoldings {
  accountId: string;
  currency: string;
  asOf: string | null;
  subPortfolios: SubPortfolioHoldings[];
}

export interface LatestPrice {
  instrumentId: string;
  date: string;
  close: number;
  adjustedClose: number;
  currency: string;
  isStale: boolean;
}

export function symbolOf(instrument: Pick<Instrument, 'providerSymbols' | 'name'>): string {
  const yahoo = instrument.providerSymbols.find((symbol) => symbol.startsWith('yfinance:'));
  return yahoo ? yahoo.slice('yfinance:'.length) : instrument.name;
}

export function today(): string {
  return toIsoDate(new Date());
}

export function toIsoDate(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function fromIsoDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}
