import { ReplayResultDto } from '../../backtesting/models/backtest-dto';
import { ResearchStudyDto } from './research-study-dto';

export interface ResearchReportDto {
  baseline: ReplayResultDto;
  studies: ResearchStudyDto[];
  parametersHash: string;
  engineVersion: string;
  selectionMetric: string;
  replayModel: string;
  instrumentLabels?: Record<string, string> | null;
}
