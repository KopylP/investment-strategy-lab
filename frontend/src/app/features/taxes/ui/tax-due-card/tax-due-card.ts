import { TranslocoPipe } from '@jsverse/transloco';
import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Button } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { TaxYearDetails } from '../../data-access/taxes-store';
import { TaxesStore } from '../../data-access/taxes-store';

@Component({
  selector: 'app-tax-due-card',
  imports: [TranslocoPipe, TaxMoneyPipe, Button, Tooltip, TapTooltip, MoneyPipe],
  templateUrl: './tax-due-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxDueCard {
  readonly details = input.required<TaxYearDetails>();
  protected readonly store = inject(TaxesStore);
}
