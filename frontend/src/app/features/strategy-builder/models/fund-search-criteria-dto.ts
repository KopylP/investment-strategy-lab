export interface FundSearchCriteriaDto {
  exchangeId: string | null;
  regionId: string | null;
  fundFamily: string | null;
  minAssets: number | null;
  maxExpenseRatio: number | null;
  nameWords: string[];
}
