import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** A tiny trend line (no axes): gain colour when the series ends higher than it started, loss colour otherwise. */
@Component({
  selector: 'app-sparkline',
  template: `
    @if (path(); as path) {
      <svg [attr.width]="width()" [attr.height]="height()" [attr.viewBox]="'0 0 ' + width() + ' ' + height()" aria-hidden="true">
        <path [attr.d]="path" fill="none" [attr.stroke]="rising() ? 'var(--p-finance-gain)' : 'var(--p-finance-loss)'" stroke-width="1.5" stroke-linejoin="round" stroke-linecap="round" />
      </svg>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sparkline {
  readonly values = input.required<number[]>();
  readonly width = input(72);
  readonly height = input(24);

  protected readonly rising = computed(() => (this.values().at(-1) ?? 0) >= (this.values().at(0) ?? 0));

  protected readonly path = computed(() => {
    const values = this.values();
    if (values.length < 2) return null;
    const min = Math.min(...values);
    const span = Math.max(...values) - min || 1;
    const [width, height] = [this.width(), this.height()];
    return values
      .map((value, index) => `${index ? 'L' : 'M'}${((index / (values.length - 1)) * (width - 2) + 1).toFixed(1)},${(height - 1 - ((value - min) / span) * (height - 2)).toFixed(1)}`)
      .join(' ');
  });
}
