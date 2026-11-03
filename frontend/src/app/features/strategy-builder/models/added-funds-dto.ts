import { FundNotAddedDto } from './fund-not-added-dto';

export interface AddedFundsDto {
  instrumentIds: string[];
  failed: FundNotAddedDto[];
}
