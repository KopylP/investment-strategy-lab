import { NodeDto } from '../../models/draft-dto';
import { VocabularyDto, VocabularyFieldDto } from '../../models/vocabulary-dto';

export interface FieldGroup {
  title: string;
  advanced?: boolean;
  fields: VocabularyFieldDto[];
}

const labels: Record<string, string> = {
  investWhenUnfavorable: 'Залишити у фондах',
  top: 'Максимум фондів у портфелі',
  order: 'Які оцінки обирати',
  method: 'Як розподілити гроші',
  maxPerFund: 'Максимум на один фонд',
  maxPerGroup: 'Максимум з однієї групи',
  maxCorrelation: 'Максимальна кореляція',
  correlationDays: 'Період кореляції, торгових днів',
  volatilityDays: 'Період ризику фонду, торгових днів',
  targetVolatility: 'Цільова річна волатильність',
  targetVolatilityDays: 'Період ризику портфеля, торгових днів',
  stopLossFromCost: 'Збиток від ціни купівлі',
  drawdownFromPeak: 'Падіння від максимуму',
};

export const fieldHints: Record<string, string> = {
  maxPerFund: 'Ліміт цільової ваги. Нерозподілений залишок іде в кеш.',
  maxCorrelation: 'Від 0 до 1: ближче до 1 — ціни фондів рухаються схожіше.',
  targetVolatility: 'Якщо розрахований ризик вищий, стратегія зменшує вкладення.',
  stopLossFromCost: 'Продати позицію, якщо вона втратила вказаний відсоток від ціни купівлі.',
  drawdownFromPeak: 'Продати позицію при вказаному падінні від її максимуму.',
};

export function resultFieldGroups(node: NodeDto, vocabulary: VocabularyDto): FieldGroup[] {
  const group = (title: string, keys: string[], advanced = false): FieldGroup => ({
    title,
    advanced,
    fields: keys.flatMap((key) => {
      const field = vocabulary.blockFields[key];
      if (!field) return [];
      return [
        {
          ...field,
          label: labels[key] ?? field.label,
          ...(key === 'maxCorrelation' ? { kind: 'Number' as const } : {}),
        },
      ];
    }),
  });
  switch (node.kind) {
    case 'MarketResult':
      return [group('Якщо умова не виконується', ['investWhenUnfavorable'])];
    case 'FundSelection':
      return [
        group('Які фонди обрати', ['top', 'order']),
        group(
          'Обмеження відбору',
          [
            'maxPerGroup',
            'maxCorrelation',
            ...(node.parameters['maxCorrelation'] != null ? ['correlationDays'] : []),
          ],
          true,
        ),
      ];
    case 'SelectionResult':
      return [
        group('Як поділити бюджет', ['method', 'maxPerFund']),
        group(
          'Обмеження ризику',
          [
            ...(node.parameters['method'] === 'inverseVolatility' ? ['volatilityDays'] : []),
            'targetVolatility',
            ...(node.parameters['targetVolatility'] != null ? ['targetVolatilityDays'] : []),
          ],
          true,
        ),
      ];
    case 'RiskResult':
      return [group('Ліміти позачергового продажу', ['stopLossFromCost', 'drawdownFromPeak'])];
    default:
      return [];
  }
}

export function isLossLimit(name: string): boolean {
  return name === 'stopLossFromCost' || name === 'drawdownFromPeak';
}
