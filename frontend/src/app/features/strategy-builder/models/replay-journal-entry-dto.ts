import { ApiSchema } from '../../../core/api/api-schema';

export type JournalEntryKind = 'rebalance' | 'risk' | 'buy' | 'sell' | 'expired';

export type ReplayJournalEntryDto = Omit<ApiSchema<'ResearchJournalEntryDto'>, 'kind'> & {
  kind: JournalEntryKind;
};
