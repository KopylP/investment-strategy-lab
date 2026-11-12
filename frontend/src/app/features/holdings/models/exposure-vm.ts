import { translate } from '@jsverse/transloco';
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

const UNKNOWN_KEY = 'Unknown';
const CASH_KEY = 'Cash';

const ASSET_CLASSES = ['Equity', 'Bond', 'Commodity', 'RealEstate'];
const REGIONS = [
  'UnitedStates',
  'Eurozone',
  'Japan',
  'DevelopedExUnitedStates',
  'EmergingMarkets',
  'Global',
];

function unknownLabel(): string {
  return translate('holdings.exposure.unknown');
}

function cashLabel(): string {
  return translate('common.cash');
}

function assetClassLabel(holding: ExposureHolding): string {
  const assetClass = holding.assetClass;
  if (!assetClass) return unknownLabel();
  if (assetClass === CASH_KEY) return cashLabel();

  return ASSET_CLASSES.includes(assetClass)
    ? translate(`holdings.exposure.assetClass.${assetClass}`)
    : assetClass;
}

function regionKey(holding: ExposureHolding): string {
  return holding.region ?? UNKNOWN_KEY;
}

function regionLabel(holding: ExposureHolding): string {
  const region = holding.region;
  if (!region) return unknownLabel();

  return REGIONS.includes(region) ? translate(`holdings.exposure.region.${region}`) : region;
}

function currencyLabel(holding: ExposureHolding): string {
  if (holding.assetCurrency) return holding.assetCurrency;

  return holding.assetClass ? translate('holdings.exposure.mixedCurrencies') : unknownLabel();
}

function addTo(buckets: Map<string, Bucket>, key: string, label: string, value: number): void {
  const bucket = buckets.get(key) ?? { label, value: 0 };
  buckets.set(key, { label, value: bucket.value + value });
}

function rowsOf(buckets: Map<string, Bucket>, total: number): ExposureRow[] {
  return [...buckets]
    .filter(([, bucket]) => bucket.value > 0)
    .map(([key, bucket]) => ({
      key,
      label: bucket.label,
      value: bucket.value,
      share: bucket.value / total,
    }))
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

  addTo(classes, cashLabel(), cashLabel(), cash);
  addTo(regions, CASH_KEY, cashLabel(), cash);
  for (const part of cashByCurrency) addTo(currencies, part.currency, part.currency, part.value);

  return [
    {
      title: translate('holdings.exposure.byAssetClass'),
      view: 'bars',
      rows: rowsOf(classes, total),
    },
    {
      title: translate('holdings.exposure.byCurrency'),
      view: 'bars',
      rows: rowsOf(currencies, total),
    },
    { title: translate('holdings.exposure.byRegion'), view: 'map', rows: rowsOf(regions, total) },
  ];
}
