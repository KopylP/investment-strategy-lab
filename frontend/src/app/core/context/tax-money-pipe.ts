import { inject, Pipe, PipeTransform } from '@angular/core';
import { formatMoney } from '../../shared/pipes/format';
import { TaxRegime } from './tax-regime';

@Pipe({ name: 'taxMoney', pure: false })
export class TaxMoneyPipe implements PipeTransform {
  private readonly regime = inject(TaxRegime);

  transform(amount: number | null | undefined, digits = 2): string {
    const currency = this.regime.currency();

    return currency ? formatMoney(amount, currency, digits) : '—';
  }
}
