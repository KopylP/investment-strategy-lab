import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MoneyPipe } from '../../pipes/format-pipes';

@Component({
  selector: 'app-trade-cost',
  imports: [TranslocoPipe, MoneyPipe],
  template: `
    <dl class="grid grid-cols-[1fr_auto] gap-x-4 gap-y-1 text-sm">
      <dt class="text-muted-color">{{ 'tradeCost.amount' | transloco }}</dt>
      <dd class="text-right">{{ amount() | money: currency() }}</dd>
      <dt class="text-muted-color">{{ 'tradeCost.fee' | transloco }}</dt>
      <dd class="text-right">{{ fee() | money: currency() }}</dd>
      <dt class="font-medium text-base pt-1">
        {{ (isSale() ? 'tradeCost.toReceive' : 'tradeCost.toPay') | transloco }}
      </dt>
      <dd class="text-right text-lg font-semibold">{{ cashChange() | money: currency() }}</dd>
    </dl>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TradeCost {
  readonly quantity = input.required<number>();
  readonly price = input.required<number>();
  readonly fee = input.required<number>();
  readonly currency = input.required<string>();
  readonly isSale = input(false);

  protected readonly amount = computed(() => this.quantity() * this.price());
  protected readonly cashChange = computed(() =>
    this.isSale() ? this.amount() - this.fee() : this.amount() + this.fee(),
  );
}
