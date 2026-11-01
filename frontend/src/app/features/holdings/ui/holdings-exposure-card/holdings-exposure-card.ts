import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PercentPipe } from '../../../../shared/pipes/format-pipes';
import { ExposureBreakdown } from '../../models/exposure-vm';

@Component({
  selector: 'app-holdings-exposure-card',
  imports: [PercentPipe],
  templateUrl: './holdings-exposure-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoldingsExposureCard {
  readonly breakdowns = input.required<ExposureBreakdown[]>();
}
