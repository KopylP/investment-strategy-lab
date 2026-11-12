import { Lot } from '../../../shared/models/api';

export interface PositionVm {
  instrumentId: string;
  symbol: string;
  name: string;
  description: string | null;
  quantity: number;
  averageCost: number;
  costBasis: number;
  price: number | null;
  quotedPrice: number | null;
  quoteCurrency: string;
  priceDate: string | null;
  isStale: boolean;
  value: number;
  pnl: number;
  pnlPct: number | null;
  weight: number;
  lots: Lot[];
}

export interface SleeveVm {
  id: string;
  name: string;
  accountName: string | null;
  targetShare: number;
  actualShare: number;
  cash: number;
  cashParts: { currency: string; amount: number }[];
  value: number;
  costBasis: number;
  pnl: number;
  positions: PositionVm[];
}

export interface HoldingsTotalsVm {
  value: number;
  cash: number;
  costBasis: number;
  pnl: number;
}
