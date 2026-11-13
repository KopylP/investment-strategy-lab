import { Instrument } from '../../../shared/models/instrument-dto';
import { SignalForm } from './signal-form';
import { activeLocale } from '../../../core/i18n/language';

export type WatchedSubject =
  | { target: 'Instrument'; instrumentId: string }
  | { target: 'InstrumentGroup'; groupName: string };

export interface InstrumentGroupSummary {
  name: string;
  instrumentCount: number;
}

export function watchedSubjectOf(form: SignalForm): WatchedSubject | null {
  if (form.target === 'Instrument' && form.instrumentId)
    return { target: 'Instrument', instrumentId: form.instrumentId };

  if (form.target === 'InstrumentGroup' && form.groupName)
    return { target: 'InstrumentGroup', groupName: form.groupName };

  return null;
}

export function instrumentGroupsOf(instruments: Instrument[]): InstrumentGroupSummary[] {
  const counts = new Map<string, number>();

  for (const { group } of instruments) {
    if (group) counts.set(group, (counts.get(group) ?? 0) + 1);
  }

  const groups = [...counts].map(([name, instrumentCount]) => ({ name, instrumentCount }));

  return groups.sort((left, right) => left.name.localeCompare(right.name, activeLocale));
}

export function instrumentMatches(instrument: Instrument, search: string): boolean {
  const { label, name, isin, group } = instrument;
  const searched = `${label} ${name} ${isin ?? ''} ${group ?? ''}`;

  return searched.toLowerCase().includes(search);
}
