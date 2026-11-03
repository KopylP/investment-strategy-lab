import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Button } from 'primeng/button';

@Component({
  selector: 'app-block-frame',
  imports: [Button],
  template: `
    <section
      class="mb-4 overflow-hidden rounded-2xl border border-surface bg-surface-0 dark:bg-surface-900"
    >
      <header class="flex items-center gap-3 border-b border-surface px-4 py-3">
        <span
          class="flex h-7 w-7 items-center justify-center rounded-full bg-surface-100 text-sm font-semibold dark:bg-surface-800"
        >
          {{ number() }}
        </span>
        <div class="min-w-0 flex-1">
          <h3 class="font-semibold">{{ title() }}</h3>
          <p class="truncate text-xs text-muted-color">{{ purpose() }}</p>
        </div>
        @if (!collapsed()) {
          <p-button
            label="Впорядкувати"
            icon="pi pi-sort-alt"
            size="small"
            [text]="true"
            severity="secondary"
            (onClick)="arrange.emit()"
          />
        }
        <p-button
          [icon]="collapsed() ? 'pi pi-chevron-down' : 'pi pi-chevron-up'"
          [ariaLabel]="collapsed() ? 'Розгорнути блок' : 'Згорнути блок'"
          size="small"
          [text]="true"
          severity="secondary"
          (onClick)="toggle.emit()"
        />
      </header>
      @if (collapsed()) {
        <div class="px-4 py-3 text-sm text-muted-color">
          {{ sentence() || 'Правила ще не задано' }}
        </div>
      } @else {
        <ng-content />
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BlockFrame {
  readonly number = input.required<number>();
  readonly title = input.required<string>();
  readonly purpose = input.required<string>();
  readonly sentence = input<string>('');
  readonly collapsed = input(false);
  readonly arrange = output<void>();
  readonly toggle = output<void>();
}
