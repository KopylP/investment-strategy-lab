import { Strategy } from './strategy';

export interface StrategyBlurb {
  /** One sentence a beginner understands: what the strategy buys and how often. */
  headline: string;
  points: string[];
  region: string;
}

const frequencyText: Record<string, string> = { weekly: 'щотижня', monthly: 'щомісяця', quarterly: 'щокварталу' };

const scheduleText: Record<string, string> = {
  weekly: 'щонеділі',
  monthly: 'першої неділі місяця',
  quarterly: 'першої неділі кварталу',
};

function read(parameters: unknown, ...path: string[]): unknown {
  let node = parameters;
  for (const key of path) node = node && typeof node === 'object' ? (node as Record<string, unknown>)[key] : undefined;
  return node;
}

/**
 * Plain-language description of a strategy from its settings, so choosing one does not require reading parameters.
 * `currencyOf` tells EU (EUR) from US (USD) universes.
 */
export function describeStrategy(strategy: Strategy, symbol: (id: string | null) => string, currencyOf: (id: string) => string | undefined): StrategyBlurb {
  const version = strategy.version;
  const count = version.universe.length;
  const currencies = new Set(version.universe.map(currencyOf).filter(Boolean));
  const region = currencies.size === 1 ? (currencies.has('EUR') ? 'європейських' : currencies.has('USD') ? 'американських' : '') : '';
  const topN = read(version.parameters, 'scanner', 'topN');
  const weak = read(version.parameters, 'weakMarketInvestPct');
  const riskReview = read(version.parameters, 'reporting', 'riskReview') ?? 'weekly';
  const rebalance = String(read(version.parameters, 'reporting', 'rebalance') ?? 'monthly');
  const frequency = frequencyText[rebalance] ?? rebalance;

  const headline = typeof topN === 'number'
    ? `${frequency[0].toUpperCase()}${frequency.slice(1)} обирає ${topN} найсильніших із ${count} ${region} ETF і тримає їх порівну`
    : `${frequency[0].toUpperCase()}${frequency.slice(1)} перебудовує портфель із ${count} ${region} ETF`;

  const points = [
    `Рапорт з купівлями й продажами — ${scheduleText[rebalance] ?? frequency}`,
    riskReview === 'off' ? 'Без щотижневої перевірки ризику' : 'Щонеділі перевіряє ризик: продає лише за сигналом виходу',
  ];
  if (typeof weak === 'number')
    points.push(`Слабкий ринок: інвестує ${Math.round(weak * 100)}% внеску${version.cashInstrumentId ? `, решта — в ${symbol(version.cashInstrumentId)}` : ''}`);
  return { headline: headline.replace(/\s+/g, ' '), points, region };
}
