import { translate } from '@jsverse/transloco';
const translatedKinds = ['Stock', 'Index', 'FxPair'];

export function instrumentKindLabel(kind: string): string {
  if (kind === 'Etf') return 'ETF';

  return translatedKinds.includes(kind) ? translate(`instrumentKind.${kind}`) : kind;
}
