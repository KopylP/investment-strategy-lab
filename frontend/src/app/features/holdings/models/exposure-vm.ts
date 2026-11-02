export interface ExposureHolding {
  value: number;
  assetClass: string | null;
  region: string | null;
  assetCurrency: string | null;
}

export interface ExposureRow {
  key: string;
  label: string;
  value: number;
  share: number;
}

export type ExposureView = 'bars' | 'map';

export interface ExposureBreakdown {
  title: string;
  view: ExposureView;
  rows: ExposureRow[];
}

interface Bucket {
  label: string;
  value: number;
}

const UNKNOWN = 'Не вказано';
const UNKNOWN_KEY = 'Unknown';
const CASH = 'Кеш';
const CASH_KEY = 'Cash';
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

function regionKey(holding: ExposureHolding): string {
  return holding.region ?? UNKNOWN_KEY;
}

function regionLabel(holding: ExposureHolding): string {
  return holding.region ? (REGIONS[holding.region] ?? holding.region) : UNKNOWN;
}

function currencyLabel(holding: ExposureHolding): string {
  if (holding.assetCurrency) return holding.assetCurrency;

  return holding.assetClass ? MIXED_CURRENCIES : UNKNOWN;
}

function addTo(buckets: Map<string, Bucket>, key: string, label: string, value: number): void {
  const bucket = buckets.get(key) ?? { label, value: 0 };
  buckets.set(key, { label, value: bucket.value + value });
}

function rowsOf(buckets: Map<string, Bucket>, total: number): ExposureRow[] {
  return [...buckets]
    .filter(([, bucket]) => bucket.value > 0)
    .map(([key, bucket]) => ({ key, label: bucket.label, value: bucket.value, share: bucket.value / total }))
    .sort((left, right) => right.value - left.value);
}

export function exposureBreakdownsOf(
  holdings: ExposureHolding[],
  cashByCurrency: { currency: string; value: number }[],
): ExposureBreakdown[] {
  const cash = cashByCurrency.reduce((sum, part) => sum + part.value, 0);
  const total = holdings.reduce((sum, holding) => sum + holding.value, 0) + cash;
  if (total <= 0) return [];

  const classes = new Map<string, Bucket>();
  const regions = new Map<string, Bucket>();
  const currencies = new Map<string, Bucket>();

  for (const holding of holdings) {
    const assetClass = assetClassLabel(holding);
    const currency = currencyLabel(holding);

    addTo(classes, assetClass, assetClass, holding.value);
    addTo(regions, regionKey(holding), regionLabel(holding), holding.value);
    addTo(currencies, currency, currency, holding.value);
  }

  addTo(classes, CASH, CASH, cash);
  addTo(regions, CASH_KEY, CASH, cash);
  for (const part of cashByCurrency) addTo(currencies, part.currency, part.currency, part.value);

  return [
    { title: 'Клас активів', view: 'bars', rows: rowsOf(classes, total) },
    { title: 'Валюта активів', view: 'bars', rows: rowsOf(currencies, total) },
    { title: 'Регіон', view: 'map', rows: rowsOf(regions, total) },
  ];
}
