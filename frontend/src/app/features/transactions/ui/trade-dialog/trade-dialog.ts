import { ChangeDetectionStrategy, Component, computed, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { toIsoDate } from '../../../../shared/models/api';
import { MoneyPipe } from '../../../../shared/pipes/format-pipes';
import { TradeInput } from '../../data-access/transactions-store';

export interface Option {
  label: string;
  value: string;
}

/** A trade done outside any report (manual entry). */
@Component({
  selector: 'app-trade-dialog',
  imports: [FormsModule, Button, DatePicker, Dialog, InputNumber, Select, SelectButton, MoneyPipe],
  templateUrl: './trade-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TradeDialog {
  readonly visible = model(false);
  readonly instruments = input.required<Option[]>();
  readonly subPortfolios = input.required<Option[]>();
  readonly currency = input.required<string>();
  readonly busy = input(false);
  readonly save = output<TradeInput>();

  protected readonly sides = [{ label: 'Купівля', value: 'Buy' }, { label: 'Продаж', value: 'Sell' }];
  protected readonly instrumentId = signal<string | null>(null);
  protected readonly side = signal<'Buy' | 'Sell'>('Buy');
  protected readonly quantity = signal<number | null>(null);
  protected readonly price = signal<number | null>(null);
  protected readonly fee = signal<number>(0);
  protected readonly executedOn = signal<Date>(new Date());
  protected readonly subPortfolioId = signal<string | null>(null);
  protected readonly total = computed(() => (this.quantity() ?? 0) * (this.price() ?? 0));
  protected readonly valid = computed(() => !!this.instrumentId() && (this.quantity() ?? 0) > 0 && (this.price() ?? 0) > 0 && this.fee() >= 0);

  protected submit(): void {
    if (!this.valid()) return;
    this.save.emit({
      instrumentId: this.instrumentId()!,
      side: this.side(),
      quantity: this.quantity()!,
      price: this.price()!,
      fee: this.fee(),
      executedOn: toIsoDate(this.executedOn()),
      subPortfolioId: this.subPortfolioId(),
    });
  }
}
