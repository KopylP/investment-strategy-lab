export interface ReplayJournalEntryDto {
  date: string;
  kind: 'rebalance' | 'risk' | 'buy' | 'sell' | 'expired';
  reason: string;
  fill: {
    instrumentId: string;
    quantity: number;
    price: number;
    referencePrice: number;
    commission: number;
    spreadCost: number;
    slippageCost: number;
    decisionDate: string;
  } | null;
  allocation: {
    cash: number;
    targets: { instrumentId: string; weight: number }[];
    positions: { instrumentId: string; quantity: number; weight: number }[];
  } | null;
  explanation: {
    summary: string;
    engineVersion: string | null;
    candidates: {
      instrumentId: string;
      name: string;
      status: string;
      reason: string;
      weight: number | null;
      values: Record<string, number | string | boolean | null> | null;
    }[] | null;
    valueLabels: Record<string, string> | null;
  } | null;
}
