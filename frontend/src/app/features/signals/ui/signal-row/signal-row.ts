import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

export type SignalRowTone = 'primary' | 'danger' | 'muted';

const toneColors: Record<SignalRowTone, string> = {
  primary: 'var(--p-primary-500)',
  danger: 'var(--p-red-500)',
  muted: 'var(--p-text-muted-color)',
};

@Component({
  selector: 'app-signal-row',
  template: `
    <button
      type="button"
      class="w-full min-h-11 text-left flex items-center gap-3 px-3 sm:px-4 py-3 border-0 border-t border-surface
        bg-transparent cursor-pointer text-color hover:bg-emphasis transition-colors"
      (click)="pick.emit()"
    >
      <span
        class="w-10 h-10 shrink-0 rounded-full flex items-center justify-center"
        [style.background]="iconBackground()"
        [style.color]="iconColor()"
        aria-hidden="true"
      >
        <i [class]="icon()"></i>
      </span>

      <span class="min-w-0 flex-1">
        <span class="block font-semibold">{{ title() }}</span>
        <span class="block text-sm text-muted-color">{{ subtitle() }}</span>
        @if (detail(); as text) {
          <span class="block text-xs text-muted-color">{{ text }}</span>
        }
      </span>

      <span class="shrink-0 text-right">
        <ng-content />
      </span>
    </button>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignalRow {
  readonly icon = input.required<string>();
  readonly tone = input<SignalRowTone>('primary');
  readonly title = input.required<string>();
  readonly subtitle = input.required<string>();
  readonly detail = input<string | null>(null);

  readonly pick = output<void>();

  protected readonly iconColor = computed(() => toneColors[this.tone()]);
  protected readonly iconBackground = computed(() => `color-mix(in srgb, ${this.iconColor()} 14%, transparent)`);
}
