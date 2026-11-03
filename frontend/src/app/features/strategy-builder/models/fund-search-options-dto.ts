import { FundSearchChoiceDto } from './fund-search-choice-dto';

export interface FundSearchOptionsDto {
  exchanges: FundSearchChoiceDto[];
  regions: FundSearchChoiceDto[];
}
