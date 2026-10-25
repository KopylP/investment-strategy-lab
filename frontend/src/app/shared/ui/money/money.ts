import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DisplayCurrency } from '../../../core/context/display-currency';
import { formatMoney } from '../../pipes/format';

/**
 * An amount shown in the user's display currency, with the original (operation/account) currency underneath when
 * they differ. `inline` puts the original next to it in brackets, for table cells and sentences.
 */
@Component({
  selector: 'app-money',
  template: `
    <span class="whitespace-nowrap">{{ main() }}</span>
    @if (original(); as original) {
      @if (inline()) {
        <span class="text-muted-color text-[0.8em] whitespace-nowrap"> ({{ original }})</span>
      } @else {
        <span class="block text-muted-color text-xs font-normal whitespace-nowrap">{{ original }}</span>
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Money {
  private readonly display = inject(DisplayCurrency);

  readonly amount = input<number | null | undefined>();
  readonly currency = input.required<string>();
  readonly digits = input(2);
  readonly inline = input(false);
  /** Hide the original amount (e.g. on a tile where it would only add noise). */
  readonly hideOriginal = input(false);

  private readonly converted = computed(() => this.display.convert(this.amount(), this.currency()));

  protected readonly main = computed(() => {
    const converted = this.converted();
    return converted === null ? formatMoney(this.amount(), this.currency(), this.digits()) : formatMoney(converted, this.display.code()!, this.digits());
  });

  protected readonly original = computed(() =>
    this.converted() !== null && this.display.code() !== this.currency() && !this.hideOriginal() ? formatMoney(this.amount(), this.currency(), this.digits()) : null,
  );
}
