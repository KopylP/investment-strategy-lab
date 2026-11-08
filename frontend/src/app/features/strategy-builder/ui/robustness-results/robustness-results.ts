import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { DatePipe, PercentPipe } from '../../../../shared/pipes/format-pipes';
import { ResearchStudyDto } from '../../models/research-study-dto';

@Component({
  selector: 'app-robustness-results',
  imports: [TranslocoPipe, DecimalPipe, DatePipe, PercentPipe],
  templateUrl: './robustness-results.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RobustnessResults {
  readonly studies = input.required<ResearchStudyDto[]>();
  protected readonly studyNames: Record<string, string> = {
    sensitivity: translate('builder.robustness.kind.neighbours'),
    'out-of-sample': translate('builder.robustness.kind.holdout'),
    'walk-forward': 'Walk-forward',
  };
}
