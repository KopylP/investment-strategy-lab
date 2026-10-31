import { computed, Directive, input } from '@angular/core';

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
