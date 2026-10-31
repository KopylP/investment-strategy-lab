import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { TableModule } from 'primeng/table';
import { MoneyPipe, QuantityPipe, DatePipe } from '../../../../shared/pipes/format-pipes';
import { TaxYearDetails } from '../../data-access/taxes-store';
import { TaxesStore } from '../../data-access/taxes-store';

@Component({
  selector: 'app-tax-open-lots-card',
  imports: [TableModule, MoneyPipe, QuantityPipe, DatePipe],
  templateUrl: './tax-open-lots-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxOpenLotsCard {
  readonly details = input.required<TaxYearDetails>();
  protected readonly store = inject(TaxesStore);
}
