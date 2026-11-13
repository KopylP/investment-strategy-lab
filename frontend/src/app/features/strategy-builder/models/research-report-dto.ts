import { ApiSchema } from '../../../core/api/api-schema';
import { ResearchStudyDto } from './research-study-dto';

export type ResearchReportDto = Omit<ApiSchema<'ResearchReportDto'>, 'studies'> & {
  studies: ResearchStudyDto[];
};
