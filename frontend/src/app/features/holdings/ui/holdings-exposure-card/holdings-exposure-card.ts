import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PercentPipe } from '../../../../shared/pipes/format-pipes';
import { ExposureBreakdown } from '../../models/exposure-vm';
import { RegionMap } from '../region-map/region-map';

@Component({
  selector: 'app-holdings-exposure-card',
  imports: [PercentPipe, RegionMap],
  templateUrl: './holdings-exposure-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoldingsExposureCard {
  readonly breakdowns = input.required<ExposureBreakdown[]>();
  readonly currency = input.required<string>();

  protected readonly bars = computed(() => this.breakdowns().filter((breakdown) => breakdown.view === 'bars'));
  protected readonly maps = computed(() => this.breakdowns().filter((breakdown) => breakdown.view === 'map'));
}
