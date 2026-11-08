import { translate } from '@jsverse/transloco';
import { Strategy } from './strategy';

export interface StrategyBlurb {
  headline: string;
  points: string[];
  region: string;
}

function read(parameters: unknown, ...path: string[]): unknown {
  let node = parameters;

  for (const key of path)
    node = node && typeof node === 'object' ? (node as Record<string, unknown>)[key] : undefined;

  return node;
}

function regionOf(currencies: Set<string | undefined>): string {
  if (currencies.size !== 1) return '';

  const [currency] = currencies;

  return currency === 'EUR' || currency === 'USD' ? translate(`strategies.blurb.region.${currency}`) : '';
}

export function describeStrategy(
  strategy: Strategy,
  symbol: (id: string | null) => string,
  currencyOf: (id: string) => string | undefined,
): StrategyBlurb {
  const version = strategy.version;
  const count = version.universe.length;
  const currencies = new Set(version.universe.map(currencyOf).filter(Boolean));
  const region = regionOf(currencies);
  const topN = read(version.parameters, 'scanner', 'topN');
  const weak = read(version.parameters, 'weakMarketInvestPct');
  const sellsPositions = read(version.parameters, 'risk', 'sellOnPositionExit') !== false;
  const maxCorrelation = read(version.parameters, 'scanner', 'maxCorrelation');
  const targetVolatility = read(version.parameters, 'targetVolatility');

  const headline =
    typeof topN === 'number'
      ? translate('strategies.blurb.picks', { topN, count, region })
      : translate('strategies.blurb.holds', { count, region });

  const points = [translate('strategies.blurb.reportPoint')];
  if (read(version.parameters, 'risk'))
    points.push(
      sellsPositions
        ? translate('strategies.blurb.riskSells')
        : translate('strategies.blurb.riskWarns'),
    );
  if (typeof maxCorrelation === 'number' && maxCorrelation < 1)
    points.push(
      translate('strategies.blurb.correlation', { limit: maxCorrelation }),
    );
  if (typeof targetVolatility === 'number' && targetVolatility < 1)
    points.push(
      version.cashInstrumentId
        ? translate('strategies.blurb.volatilityCash', {
            target: Math.round(targetVolatility * 100),
            cash: symbol(version.cashInstrumentId),
          })
        : translate('strategies.blurb.volatilityNoCash', { target: Math.round(targetVolatility * 100) }),
    );
  if (typeof weak === 'number')
    points.push(
      version.cashInstrumentId
        ? translate('strategies.blurb.weakMarketCash', {
            share: Math.round(weak * 100),
            cash: symbol(version.cashInstrumentId),
          })
        : translate('strategies.blurb.weakMarket', { share: Math.round(weak * 100) }),
    );

  return { headline: headline.replace(/\s+/g, ' '), points, region };
}
