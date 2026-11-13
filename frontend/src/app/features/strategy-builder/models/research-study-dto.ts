import { ApiSchema } from '../../../core/api/api-schema';

export type ResearchStudyKind = 'sensitivity' | 'out-of-sample' | 'walk-forward';

export type ResearchStudyDto = Omit<ApiSchema<'ResearchStudyDto'>, 'kind'> & {
  kind: ResearchStudyKind;
};
