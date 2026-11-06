import { ReplayJournalEntryDto } from './replay-journal-entry-dto';

export interface JournalPageDto {
  total: number;
  offset: number;
  entries: ReplayJournalEntryDto[];
}
