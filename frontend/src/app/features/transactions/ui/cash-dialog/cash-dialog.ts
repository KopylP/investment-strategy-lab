import { ChangeDetectionStrategy, Component, computed, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { toIsoDate } from '../../../../shared/models/api';
import { CashInput } from '../../data-access/transactions-store';
import { Option } from '../trade-dialog/trade-dialog';

@Component({
  selector: 'app-cash-dialog',
  imports: [FormsModule, Button, DatePicker, Dialog, InputNumber, Select],
  templateUrl: './cash-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CashDialog {
  readonly visible = model(false);
  readonly instruments = input.required<Option[]>();
  readonly subPortfolios = input.required<Option[]>();
  readonly currency = input.required<string>();
  readonly busy = input(false);
  readonly save = output<CashInput>();

  protected readonly kinds: { label: string; value: CashInput['kind'] }[] = [
    { label: 'Внесок', value: 'Contribution' },
    { label: 'Вивід', value: 'Withdrawal' },
    { label: 'Дивіденд', value: 'Dividend' },
    { label: 'Комісія брокера', value: 'Fee' },
    { label: 'Сплата податку', value: 'TaxPayment' },
  ];
  protected readonly kind = signal<CashInput['kind']>('Contribution');
  protected readonly amount = signal<number | null>(null);
  protected readonly occurredOn = signal<Date>(new Date());
  protected readonly subPortfolioId = signal<string | null>(null);
  protected readonly instrumentId = signal<string | null>(null);
  protected readonly valid = computed(() => (this.amount() ?? 0) > 0 && (this.kind() !== 'Dividend' || !!this.instrumentId()));

  protected submit(): void {
    if (!this.valid()) return;
    this.save.emit({
      kind: this.kind(),
      amount: this.amount()!,
      occurredOn: toIsoDate(this.occurredOn()),
      subPortfolioId: this.subPortfolioId(),
      instrumentId: this.kind() === 'Dividend' ? this.instrumentId() : null,
    });
  }
}
