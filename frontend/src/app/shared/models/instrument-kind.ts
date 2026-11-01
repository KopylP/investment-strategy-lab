const kindLabels: Record<string, string> = {
  Etf: 'ETF',
  Stock: 'Акція',
  Index: 'Індекс',
  FxPair: 'Валюта',
};

export function instrumentKindLabel(kind: string): string {
  return kindLabels[kind] ?? kind;
}
