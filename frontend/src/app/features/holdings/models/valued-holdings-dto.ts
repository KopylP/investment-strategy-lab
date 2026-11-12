import { Lot } from '../../../shared/models/api';

export interface ValuedHoldingsDto {
  accountId: string;
  currency: string;
  asOf: string | null;
  value: number;
  cash: number;
  costBasis: number;
  result: number;
  resultShare: number | null;
  subPortfolios: ValuedSubPortfolioDto[];
}

export interface ValuedSubPortfolioDto {
  subPortfolioId: string;
  name: string;
  cash: number;
  cashBalances: CashBalanceDto[];
  value: number;
  costBasis: number;
  result: number;
  share: number;
  positions: ValuedPositionDto[];
}

export interface ValuedPositionDto {
  instrumentId: string;
  quantity: number;
  averageCost: number;
  costBasis: number;
  price: number | null;
  quotedPrice: number | null;
  quoteCurrency: string | null;
  priceDate: string | null;
  isStale: boolean;
  value: number;
  result: number;
  resultShare: number | null;
  weight: number;
  lots: Lot[];
}

export interface CashBalanceDto {
  currency: string;
  amount: number;
}
