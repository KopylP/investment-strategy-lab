import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { MoneyPipe, PercentPipe } from '../../../../shared/pipes/format-pipes';
import { ExposureRow } from '../../models/exposure-vm';
import { REGION_PLACES, RegionPlace } from './region-places';
import { LAND_DOT_SPACING, WORLD_HEIGHT, WORLD_LAND_DOTS, WORLD_TOP_LATITUDE, WORLD_WIDTH } from './world-land-dots';

const LARGEST_RADIUS = 24;
const SMALLEST_RADIUS = 3;
const HIT_MARGIN = 5;
const LABEL_GAP = 2;

interface RegionBubble {
  row: ExposureRow;
  x: number;
  y: number;
  radius: number;
  hitRadius: number;
  leftPercent: number;
  topPercent: number;
  labelTopPercent: number;
}

@Component({
  selector: 'app-region-map',
  imports: [PercentPipe, MoneyPipe],
  templateUrl: './region-map.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegionMap {
  readonly rows = input.required<ExposureRow[]>();
  readonly currency = input.required<string>();

  protected readonly viewBox = `0 0 ${WORLD_WIDTH} ${WORLD_HEIGHT}`;
  protected readonly aspectRatio = `${WORLD_WIDTH} / ${WORLD_HEIGHT}`;
  protected readonly land = WORLD_LAND_DOTS;
  protected readonly dotDash = `0 ${LAND_DOT_SPACING}`;
  protected readonly hovered = signal<RegionBubble | null>(null);

  protected readonly bubbles = computed(() => this.placedRows().map((row) => bubbleOf(row, REGION_PLACES[row.key])));

  protected readonly unplaced = computed(() => this.rows().filter((row) => !(row.key in REGION_PLACES)));

  protected readonly summary = computed(() =>
    this.rows()
      .map((row) => `${row.label} ${Math.round(row.share * 100)}%`)
      .join(', '),
  );

  private readonly placedRows = computed(() => this.rows().filter((row) => row.key in REGION_PLACES));
}

function bubbleOf(row: ExposureRow, place: RegionPlace): RegionBubble {
  const x = place.longitude + WORLD_WIDTH / 2;
  const y = WORLD_TOP_LATITUDE - place.latitude;
  const radius = Math.max(SMALLEST_RADIUS, LARGEST_RADIUS * Math.sqrt(row.share));

  return {
    row,
    x,
    y,
    radius,
    hitRadius: radius + HIT_MARGIN,
    leftPercent: (x / WORLD_WIDTH) * 100,
    topPercent: ((y - radius) / WORLD_HEIGHT) * 100,
    labelTopPercent: ((y + radius + LABEL_GAP) / WORLD_HEIGHT) * 100,
  };
}
