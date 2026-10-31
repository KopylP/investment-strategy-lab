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

export function describeStrategy(
  strategy: Strategy,
  symbol: (id: string | null) => string,
  currencyOf: (id: string) => string | undefined,
): StrategyBlurb {
  const version = strategy.version;
  const count = version.universe.length;
  const currencies = new Set(version.universe.map(currencyOf).filter(Boolean));
  const region =
    currencies.size === 1
      ? currencies.has('EUR')
        ? 'європейських'
        : currencies.has('USD')
          ? 'американських'
          : ''
      : '';
  const topN = read(version.parameters, 'scanner', 'topN');
  const weak = read(version.parameters, 'weakMarketInvestPct');

  const headline =
    typeof topN === 'number'
      ? `Обирає ${topN} найсильніших із ${count} ${region} ETF і тримає їх порівну`
      : `Перебудовує портфель із ${count} ${region} ETF`;

  const points = [
    'Рапорт з купівлями й продажами — коли вам зручно: розклад задаєте в рахунку',
    'Між ребалансами перевіряє ризик: продає лише за сигналом виходу',
  ];
  if (typeof weak === 'number')
    points.push(
      `Слабкий ринок: інвестує ${Math.round(weak * 100)}% внеску${version.cashInstrumentId ? `, решта — в ${symbol(version.cashInstrumentId)}` : ''}`,
    );

  return { headline: headline.replace(/\s+/g, ' '), points, region };
}
