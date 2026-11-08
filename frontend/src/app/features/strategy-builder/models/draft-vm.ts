import { translate } from '@jsverse/transloco';
import { BlockKind } from './draft-dto';
import { builtInSignalKindLabels } from '../../signals/models/signal-labels';

export type EditorMode = 'steps' | 'graph' | 'json';
export type SaveState = 'saved' | 'pending' | 'saving';

export interface BlockVm {
  kind: BlockKind;
  number: number;
  title: string;
  purpose: string;
  result: string;
}

export const blocks: BlockVm[] = [
  {
    kind: 'MarketFilter',
    number: 1,
    title: translate('builder.block.market.title'),
    purpose: translate('builder.block.market.scope'),
    result: translate('builder.block.market.outcome'),
  },
  {
    kind: 'Exit',
    number: 2,
    title: translate('builder.block.exit.title'),
    purpose: translate('builder.block.exit.scope'),
    result: translate('builder.block.exit.outcome'),
  },
  {
    kind: 'Entry',
    number: 3,
    title: translate('builder.block.entry.title'),
    purpose: translate('builder.block.entry.scope'),
    result: translate('builder.block.entry.outcome'),
  },
  {
    kind: 'Selection',
    number: 4,
    title: translate('builder.block.selection.title'),
    purpose: translate('builder.block.selection.scope'),
    result: translate('builder.block.selection.outcome'),
  },
  {
    kind: 'Risk',
    number: 5,
    title: translate('builder.block.risk.title'),
    purpose: translate('builder.block.exit.scope'),
    result: translate('builder.block.risk.outcome'),
  },
];

export const builtInSignalKinds = Object.entries(builtInSignalKindLabels).map(([value, label]) => ({
  value,
  label,
}));

export interface SetupSummaryVm {
  fundCount: number;
  fundLabels: string[];
  moreFunds: number;
  benchmark: string;
  cash: string;
  reports: string;
}
