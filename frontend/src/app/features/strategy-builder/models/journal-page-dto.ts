import { ApiSchema } from '../../../core/api/api-schema';
import { ReplayJournalEntryDto } from './replay-journal-entry-dto';

export type JournalPageDto = Omit<ApiSchema<'ExperimentJournalDto'>, 'entries'> & {
  entries: ReplayJournalEntryDto[];
};
