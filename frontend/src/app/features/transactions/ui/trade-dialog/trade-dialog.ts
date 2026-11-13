import { TranslocoPipe, translate } from '@jsverse/transloco';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { SelectButton } from 'primeng/selectbutton';
import { toIsoDate } from '../../../../shared/models/iso-date';
import { TradeCost } from '../../../../shared/ui/trade-cost/trade-cost';
import { NewTrade } from '../../data-access/transactions-store';

export interface Option {
  label: string;
  value: string;
}

@Component({
  selector: 'app-trade-dialog',
  imports: [
    TranslocoPipe,
    FormsModule,
    Button,
    DatePicker,
    Dialog,
    InputNumber,
    Select,
    SelectButton,
    TradeCost,
  ],
  templateUrl: './trade-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TradeDialog {
  readonly visible = model(false);
  readonly instruments = input.required<Option[]>();
  readonly subPortfolios = input.required<Option[]>();
  readonly currency = input.required<string>();
  readonly busy = input(false);
  readonly save = output<NewTrade>();

  protected readonly sides = [
    { label: translate('chart.buy'), value: 'Buy' },
    { label: translate('chart.sell'), value: 'Sell' },
  ];
  protected readonly instrumentId = signal<string | null>(null);
  protected readonly side = signal<'Buy' | 'Sell'>('Buy');
  protected readonly quantity = signal<number | null>(null);
  protected readonly price = signal<number | null>(null);
  protected readonly fee = signal<number>(0);
  protected readonly executedOn = signal<Date>(new Date());
  protected readonly subPortfolioId = signal<string | null>(null);
  protected readonly isSale = computed(() => this.side() === 'Sell');
  protected readonly subPortfolioRequired = computed(
    () => !this.isSale() && this.subPortfolios().length > 1,
  );
  protected readonly subPortfolioPlaceholder = computed(() =>
    this.isSale() ? 'transactions.trade.autoSplitSell' : 'transactions.trade.chooseSubPortfolio',
  );
  protected readonly valid = computed(
    () =>
      !!this.instrumentId() &&
      (this.quantity() ?? 0) > 0 &&
      (this.price() ?? 0) > 0 &&
      this.fee() >= 0 &&
      this.hasSubPortfolioWhenRequired(),
  );

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

  private hasSubPortfolioWhenRequired(): boolean {
    return !this.subPortfolioRequired() || !!this.subPortfolioId();
  }
}
