import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { Money } from '../../../../shared/ui/money/money';
import { MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { HoldingsStore } from '../../data-access/holdings-store';

@Component({
  selector: 'app-holdings-tiles',
  imports: [Tooltip, TapTooltip, Money, MoneyPipe],
  templateUrl: './holdings-tiles.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoldingsTiles {
  readonly totals = input.required<ReturnType<HoldingsStore['totals']>>();
  protected readonly store = inject(HoldingsStore);
}
