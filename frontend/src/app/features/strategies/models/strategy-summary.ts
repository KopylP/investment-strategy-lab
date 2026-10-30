import { ReportTiming, scheduleLabel } from '../../reports/models/report-schedule';
import { Strategy } from './strategy';

export interface StrategyBlurb {
  /** One sentence a beginner understands: what the strategy buys and how often. */
  headline: string;
  points: string[];
  region: string;
}

const frequencyText: Record<string, string> = { hourly: 'щогодини', daily: 'щодня', weekly: 'щотижня', monthly: 'щомісяця', quarterly: 'щокварталу' };

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
  const timing = (name: string, fallback: ReportTiming): ReportTiming => ({ ...fallback, ...(read(version.parameters, 'reporting', name) as Partial<ReportTiming> | undefined) });
  const rebalance = timing('rebalance', { frequency: 'monthly', day: 1, weekday: 'sunday', time: '20:00', minute: 0 });
  const riskReview = timing('riskReview', { frequency: 'weekly', day: 1, weekday: 'sunday', time: '19:00', minute: 0 });
  const frequency = frequencyText[rebalance.frequency] ?? rebalance.frequency;

  const headline = typeof topN === 'number'
    ? `${frequency[0].toUpperCase()}${frequency.slice(1)} обирає ${topN} найсильніших із ${count} ${region} ETF і тримає їх порівну`
    : `${frequency[0].toUpperCase()}${frequency.slice(1)} перебудовує портфель із ${count} ${region} ETF`;

  const points = [
    `Рапорт з купівлями й продажами — ${scheduleLabel(rebalance)}`,
    riskReview.frequency === 'off' ? 'Без перевірки ризику між ребалансами' : `Перевіряє ризик ${scheduleLabel(riskReview)}: продає лише за сигналом виходу`,
  ];
  if (typeof weak === 'number')
    points.push(`Слабкий ринок: інвестує ${Math.round(weak * 100)}% внеску${version.cashInstrumentId ? `, решта — в ${symbol(version.cashInstrumentId)}` : ''}`);
  return { headline: headline.replace(/\s+/g, ' '), points, region };
}
