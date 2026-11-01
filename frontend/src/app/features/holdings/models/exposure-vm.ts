export interface ExposureHolding {
  value: number;
  assetClass: string | null;
  region: string | null;
  assetCurrency: string | null;
}

export interface ExposureRow {
  label: string;
  value: number;
  share: number;
}

export interface ExposureBreakdown {
  title: string;
  rows: ExposureRow[];
}

const UNKNOWN = 'Не вказано';
const CASH = 'Кеш';
const MIXED_CURRENCIES = 'Кілька валют';

const ASSET_CLASSES: Record<string, string> = {
  Equity: 'Акції',
  Bond: 'Облігації',
  Commodity: 'Сировина',
  RealEstate: 'Нерухомість',
  Cash: CASH,
};

const REGIONS: Record<string, string> = {
  UnitedStates: 'США',
  Eurozone: 'Єврозона',
  Japan: 'Японія',
  DevelopedExUnitedStates: 'Розвинені поза США',
  EmergingMarkets: 'Ринки, що розвиваються',
  Global: 'Світ',
};

function assetClassLabel(holding: ExposureHolding): string {
  return holding.assetClass ? (ASSET_CLASSES[holding.assetClass] ?? holding.assetClass) : UNKNOWN;
}

function regionLabel(holding: ExposureHolding): string {
  return holding.region ? (REGIONS[holding.region] ?? holding.region) : UNKNOWN;
}

function currencyLabel(holding: ExposureHolding): string {
  if (holding.assetCurrency) return holding.assetCurrency;

  return holding.assetClass ? MIXED_CURRENCIES : UNKNOWN;
}

function addTo(values: Map<string, number>, label: string, value: number): void {
  values.set(label, (values.get(label) ?? 0) + value);
}

function rowsOf(values: Map<string, number>, total: number): ExposureRow[] {
  return [...values]
    .filter(([, value]) => value > 0)
    .map(([label, value]) => ({ label, value, share: value / total }))
    .sort((left, right) => right.value - left.value);
}

export function exposureBreakdownsOf(
  holdings: ExposureHolding[],
  cashByCurrency: { currency: string; value: number }[],
): ExposureBreakdown[] {
  const cash = cashByCurrency.reduce((sum, part) => sum + part.value, 0);
  const total = holdings.reduce((sum, holding) => sum + holding.value, 0) + cash;
  if (total <= 0) return [];

  const classes = new Map<string, number>();
  const regions = new Map<string, number>();
  const currencies = new Map<string, number>();

  for (const holding of holdings) {
    addTo(classes, assetClassLabel(holding), holding.value);
    addTo(regions, regionLabel(holding), holding.value);
    addTo(currencies, currencyLabel(holding), holding.value);
  }

  addTo(classes, CASH, cash);
  addTo(regions, CASH, cash);
  for (const part of cashByCurrency) addTo(currencies, part.currency, part.value);

  return [
    { title: 'Клас активів', rows: rowsOf(classes, total) },
    { title: 'Регіон', rows: rowsOf(regions, total) },
    { title: 'Валюта активів', rows: rowsOf(currencies, total) },
  ];
}
