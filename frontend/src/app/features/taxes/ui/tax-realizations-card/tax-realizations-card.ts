import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { MoneyPipe, QuantityPipe, DatePipe } from '../../../../shared/pipes/format-pipes';
import { Realization, TaxYearDetails } from '../../data-access/taxes-store';
import { TaxesStore } from '../../data-access/taxes-store';

@Component({
  selector: 'app-tax-realizations-card',
  imports: [
    TaxMoneyPipe,
    Button,
    Message,
    TableModule,
    Tooltip,
    TapTooltip,
    SignedValue,
    MoneyPipe,
    QuantityPipe,
    DatePipe,
  ],
  templateUrl: './tax-realizations-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxRealizationsCard {
  readonly details = input.required<TaxYearDetails>();
  protected readonly store = inject(TaxesStore);

  protected brokerDifference(sale: Realization): boolean {
    return sale.brokerGainPln !== null && Math.abs(sale.gainPln - sale.brokerGainPln) >= 1;
  }
}
