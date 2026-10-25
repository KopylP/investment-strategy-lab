import { computed, Directive, input } from '@angular/core';

/** Gain/loss colour from the theme tokens (--p-finance-gain / --p-finance-loss), never a hard-coded colour. */
@Directive({
  selector: '[appSignedValue]',
  host: { '[style.color]': 'color()' },
})
export class SignedValue {
  readonly appSignedValue = input<number | null | undefined>();

  protected readonly color = computed(() => {
    const value = this.appSignedValue();
    if (value === null || value === undefined || value === 0) return null;
    return value > 0 ? 'var(--p-finance-gain)' : 'var(--p-finance-loss)';
  });
}
