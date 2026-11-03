import { BlockWordsDto } from './draft-dto';

export interface PublishedStrategyDto {
  editable: boolean;
  draftId: string | null;
  words: BlockWordsDto[];
}
