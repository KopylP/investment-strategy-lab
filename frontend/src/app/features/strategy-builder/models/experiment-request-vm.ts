import { ExperimentConditionsDto } from './experiment-conditions-dto';

export interface ExperimentRequestVm {
  label: string | null;
  experiment: ExperimentConditionsDto;
}
