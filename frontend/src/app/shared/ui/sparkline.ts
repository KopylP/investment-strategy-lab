import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-sparkline',
  template: `
    @if (path(); as path) {
      <svg
        [attr.width]="width()"
        [attr.height]="height()"
        [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
        aria-hidden="true"
      >
        <path
          [attr.d]="path"
          fill="none"
          [attr.stroke]="rising() ? 'var(--p-finance-gain)' : 'var(--p-finance-loss)'"
          stroke-width="1.5"
          stroke-linejoin="round"
          stroke-linecap="round"
        />
      </svg>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Sparkline {
  readonly values = input.required<number[]>();
  readonly width = input(72);
  readonly height = input(24);

  protected readonly rising = computed(
    () => (this.values().at(-1) ?? 0) >= (this.values().at(0) ?? 0),
  );

  protected readonly path = computed(() => {
    const values = this.values();
    if (values.length < 2) return null;

    const min = Math.min(...values);
    const span = Math.max(...values) - min || 1;
    const [width, height] = [this.width(), this.height()];

    const toPathStep = (value: number, index: number) => {
      const command = index ? 'L' : 'M';
      const x = (index / (values.length - 1)) * (width - 2) + 1;
      const y = height - 1 - ((value - min) / span) * (height - 2);

      return `${command}${x.toFixed(1)},${y.toFixed(1)}`;
    };

    return values.map(toPathStep).join(' ');
  });
}
