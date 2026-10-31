import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { Tag } from 'primeng/tag';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { Strategy } from '../../models/strategy';
import { describeStrategy } from '../../models/strategy-summary';

@Component({
  selector: 'app-strategy-picker',
  imports: [Tag],
  template: `
    <div class="grid gap-3" [class.md:grid-cols-2]="!compact()">
      @for (card of cards(); track card.id) {
        <button type="button" (click)="valueChange.emit(card.id)"
          class="text-left rounded-border border p-4 bg-transparent cursor-pointer text-color transition-colors hover:border-primary"
          [class.border-primary]="value() === card.id" [class.border-surface]="value() !== card.id"
          [style.background]="value() === card.id ? 'color-mix(in srgb, var(--p-primary-500) 8%, transparent)' : null">
          <div class="flex items-start justify-between gap-2">
            <div class="font-semibold">{{ card.name }}</div>
            @if (value() === card.id) { <i class="pi pi-check-circle text-primary"></i> }
          </div>
          <div class="text-sm mt-1">{{ card.blurb.headline }}</div>
          @if (!compact()) {
            <ul class="text-xs text-muted-color mt-2 mb-0 pl-4">
              @for (point of card.blurb.points; track point) { <li>{{ point }}</li> }
            </ul>
          }
          <div class="flex flex-wrap gap-1 mt-2">
            @for (ticker of card.tickers; track ticker) { <p-tag [value]="ticker" severity="secondary" styleClass="text-xs" /> }
          </div>
        </button>
      }
      @if (allowNone()) {
        <button type="button" (click)="valueChange.emit(null)"
          class="text-left rounded-border border p-4 bg-transparent cursor-pointer text-color transition-colors hover:border-primary"
          [class.border-primary]="value() === null" [class.border-surface]="value() !== null">
          <div class="font-semibold">Без стратегії</div>
          <div class="text-sm text-muted-color mt-1">Під-портфель просто тримає те, що є; рапорти його не чіпають.</div>
        </button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyPicker {
  private readonly catalog = inject(InstrumentCatalog);

  readonly strategies = input.required<Strategy[]>();
  readonly value = input<string | null>(null);
  readonly allowNone = input(false);
  readonly compact = input(false);
  readonly valueChange = output<string | null>();

  protected readonly cards = computed(() =>
    this.strategies()
      .filter((strategy) => strategy.status === 'Active')
      .map((strategy) => ({
        id: strategy.id,
        name: strategy.name,
        blurb: describeStrategy(strategy, (id) => this.catalog.symbol(id), (id) => this.catalog.byId().get(id)?.tradingCurrency),
        tickers: [...strategy.version.universe.slice(0, 6).map((id) => this.catalog.symbol(id)), ...(strategy.version.universe.length > 6 ? [`+${strategy.version.universe.length - 6}`] : [])],
      })),
  );
}
