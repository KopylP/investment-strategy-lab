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
    title: 'Ринковий фільтр',
    purpose: 'для ринку загалом',
    result: 'Ринок сприятливий',
  },
  {
    kind: 'Exit',
    number: 2,
    title: 'Вихід',
    purpose: 'для кожного фонду в портфелі',
    result: 'Продати',
  },
  {
    kind: 'Entry',
    number: 3,
    title: 'Вхід',
    purpose: 'для кожного фонду поза портфелем',
    result: 'Кандидат на купівлю',
  },
  {
    kind: 'Selection',
    number: 4,
    title: 'Відбір і ваги',
    purpose: 'серед кандидатів',
    result: 'Розподілити гроші',
  },
  {
    kind: 'Risk',
    number: 5,
    title: 'Ризик між ребалансами',
    purpose: 'для кожного фонду в портфелі',
    result: 'Продати позачергово',
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
