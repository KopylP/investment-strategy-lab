import { translate } from '@jsverse/transloco';
import { NodeDto } from '../../models/draft-dto';
import { VocabularyDto, VocabularyFieldDto } from '../../models/vocabulary-dto';

export interface FieldGroup {
  title: string;
  advanced?: boolean;
  fields: VocabularyFieldDto[];
}

const labels: Record<string, string> = {
  whenUnfavorable: translate('builder.inspector.fields.heldFunds'),
  investWhenUnfavorable: translate('builder.inspector.fields.keepInFunds'),
  top: translate('builder.inspector.fields.maxFunds'),
  order: translate('builder.inspector.fields.whichScores'),
  method: translate('builder.inspector.fields.splitMoney'),
  maxPerFund: translate('builder.inspector.fields.maxPerFund'),
  maxPerGroup: translate('builder.inspector.fields.maxPerGroup'),
  maxCorrelation: translate('builder.inspector.fields.maxCorrelation'),
  correlationDays: translate('builder.inspector.fields.correlationDays'),
  volatilityDays: translate('builder.inspector.fields.fundRiskDays'),
  targetVolatility: translate('builder.inspector.fields.targetVolatility'),
  targetVolatilityDays: translate('builder.inspector.fields.portfolioRiskDays'),
  stopLossFromCost: translate('builder.inspector.fields.lossFromCost'),
  drawdownFromPeak: translate('builder.inspector.fields.fallFromPeak'),
};

export const fieldHints: Record<string, string> = {
  whenUnfavorable: translate('builder.inspector.fields.heldFundsHint'),
  maxPerFund: translate('builder.inspector.fields.maxPerFundHint'),
  maxCorrelation: translate('builder.inspector.fields.correlationHint'),
  targetVolatility: translate('builder.inspector.fields.volatilityHint'),
  stopLossFromCost: translate('builder.inspector.fields.lossHint'),
  drawdownFromPeak: translate('builder.inspector.fields.fallHint'),
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
      return [
        group(translate('builder.inspector.fields.section.unfavorable'), [
          'whenUnfavorable',
          'investWhenUnfavorable',
        ]),
      ];
    case 'FundSelection':
      return [
        group(translate('builder.inspector.fields.section.which'), ['top', 'order']),
        group(
          translate('builder.inspector.fields.section.limits'),
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
        group(translate('builder.inspector.fields.section.split'), ['method', 'maxPerFund']),
        group(
          translate('builder.inspector.fields.section.risk'),
          [
            ...(node.parameters['method'] === 'inverseVolatility' ? ['volatilityDays'] : []),
            'targetVolatility',
            ...(node.parameters['targetVolatility'] != null ? ['targetVolatilityDays'] : []),
          ],
          true,
        ),
      ];
    case 'RiskResult':
      return [
        group(translate('builder.inspector.fields.section.exitLimits'), [
          'stopLossFromCost',
          'drawdownFromPeak',
        ]),
      ];
    default:
      return [];
  }
}

export function isLossLimit(name: string): boolean {
  return name === 'stopLossFromCost' || name === 'drawdownFromPeak';
}
