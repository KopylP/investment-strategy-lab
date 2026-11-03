export interface FoundFundDto {
  symbol: string;
  name: string;
  currency: string | null;
  fundFamily: string | null;
  totalAssets: number | null;
  expenseRatio: number | null;
  instrumentId: string | null;
}
