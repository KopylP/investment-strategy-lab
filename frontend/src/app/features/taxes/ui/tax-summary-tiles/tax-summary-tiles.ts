import { TranslocoPipe } from '@jsverse/transloco';
import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { TaxYearSummary } from '../../data-access/taxes-store';

@Component({
  selector: 'app-tax-summary-tiles',
  imports: [TranslocoPipe, TaxMoneyPipe, SignedValue, MoneyPipe],
  templateUrl: './tax-summary-tiles.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxSummaryTiles {
  readonly summary = input.required<TaxYearSummary>();
}
