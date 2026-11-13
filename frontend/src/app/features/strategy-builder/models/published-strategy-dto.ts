import { ApiSchema } from '../../../core/api/api-schema';
import { BlockWordsDto } from './draft-dto';

export type PublishedStrategyDto = Omit<ApiSchema<'PublishedStrategyDto'>, 'words'> & {
  words: BlockWordsDto[];
};
