import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { Money } from '../../../../shared/ui/money/money';
import { PercentPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { HoldingsStore } from '../../data-access/holdings-store';

@Component({
  selector: 'app-holdings-summary-card',
  imports: [Tooltip, TapTooltip, SignedValue, Money, PercentPipe, SignedPercentPipe],
  templateUrl: './holdings-summary-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoldingsSummaryCard {
  readonly totals = input.required<ReturnType<HoldingsStore['totals']>>();
  protected readonly store = inject(HoldingsStore);
}
