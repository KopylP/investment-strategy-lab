import { TranslocoPipe } from '@jsverse/transloco';
import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { TableModule } from 'primeng/table';
import { MoneyPipe, DatePipe } from '../../../../shared/pipes/format-pipes';
import { TaxYearDetails } from '../../models/tax-dto';
import { TaxesStore } from '../../data-access/taxes-store';

@Component({
  selector: 'app-tax-dividends-card',
  imports: [TranslocoPipe, TaxMoneyPipe, TableModule, MoneyPipe, DatePipe],
  templateUrl: './tax-dividends-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxDividendsCard {
  readonly details = input.required<TaxYearDetails>();
  protected readonly store = inject(TaxesStore);
}
