import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MultiSelect } from 'primeng/multiselect';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { activeLocale } from '../../../../core/i18n/language';

interface FundGroup {
  label: string;
  items: InstrumentChoiceDto[];
}

@Component({
  selector: 'app-builder-fund-picker',
  imports: [TranslocoPipe, FormsModule, MultiSelect],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p-multiselect
      [inputId]="inputId()"
      [options]="groups()"
      [group]="true"
      optionGroupLabel="label"
      optionGroupChildren="items"
      optionLabel="label"
      optionValue="id"
      [ngModel]="value()"
      (ngModelChange)="valueChange.emit($event ?? [])"
      [filter]="true"
      filterBy="label,name"
      [filterPlaceHolder]="'builder.fundPicker.search' | transloco"
      [showToggleAll]="false"
      [selectionLimit]="60"
      [maxSelectedLabels]="0"
      [selectedItemsLabel]="'builder.fundPicker.selectedCount' | transloco"
      [placeholder]="'builder.fundPicker.placeholder' | transloco"
      [size]="compact() ? 'small' : undefined"
      [appendTo]="overlayContainer()"
      panelStyleClass="builder-fund-picker-panel"
      [panelStyle]="{ minWidth: '22rem', maxWidth: 'min(32rem, 95vw)' }"
      fluid
    >
      <ng-template #group let-group>
        <button
          type="button"
          role="checkbox"
          class="fund-group-toggle"
          [attr.aria-label]="'builder.fundPicker.wholeGroup' | transloco: { group: group.label }"
          [attr.aria-checked]="groupState(group)"
          [disabled]="exceedsLimit(group)"
          [title]="
            exceedsLimit(group)
              ? ('builder.fundPicker.limitReached' | transloco)
              : ('builder.fundPicker.toggleGroup' | transloco)
          "
          (pointerdown)="$event.stopPropagation()"
          (click)="toggleGroup($event, group)"
        >
          <span class="group-check" [class.checked]="groupState(group) !== 'false'">
            @if (groupState(group) === 'true') {
              <i class="pi pi-check"></i>
            } @else if (groupState(group) === 'mixed') {
              <i class="pi pi-minus"></i>
            }
          </span>
          <span class="group-label"
            ><i class="pi pi-folder" aria-hidden="true"></i>{{ group.label }}</span
          >
          <span class="group-count">{{ selectedIn(group) }} / {{ group.items.length }}</span>
        </button>
      </ng-template>
      <ng-template #item let-item>
        <span class="fund-option"
          ><strong>{{ item.label }}</strong
          ><span>{{ item.name }}</span></span
        >
      </ng-template>
      <ng-template #footer>
        <div class="fund-picker-footer">
          {{ 'builder.fundPicker.chosenCount' | transloco: { value: value().length } }}
        </div>
      </ng-template>
    </p-multiselect>
  `,
  styles: [
    `
      :host {
        display: block;
        min-width: 0;
      }
      .fund-group-toggle {
        display: flex;
        align-items: center;
        gap: 10px;
        width: 100%;
        padding: 9px 10px;
        border-radius: 6px;
        background: var(--p-content-hover-background);
        border: 1px solid var(--p-content-border-color);
        cursor: pointer;
        color: var(--p-text-color);
        text-align: left;
      }
      .fund-group-toggle:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
      .fund-group-toggle:focus-visible {
        outline: 2px solid var(--p-primary-color);
        outline-offset: 3px;
      }
      .group-check {
        display: grid;
        place-items: center;
        width: 18px;
        height: 18px;
        border: 1px solid var(--p-form-field-border-color);
        border-radius: 4px;
        flex-shrink: 0;
        background: var(--p-content-background);
      }
      .group-check.checked {
        background: var(--p-primary-color);
        border-color: var(--p-primary-color);
        color: var(--p-primary-contrast-color);
      }
      .group-check i {
        font-size: 11px;
      }
      .group-label {
        display: flex;
        align-items: center;
        gap: 7px;
        flex: 1;
        font-size: 12px;
        font-weight: 600;
        color: var(--p-text-muted-color);
      }
      .group-label i {
        font-size: 12px;
      }
      .group-count {
        font-size: 11px;
        font-variant-numeric: tabular-nums;
        color: var(--p-text-muted-color);
        border: 1px solid var(--p-content-border-color);
        border-radius: 10px;
        padding: 2px 6px;
        background: var(--p-content-background);
        white-space: nowrap;
      }
      .fund-option {
        display: flex;
        flex-direction: column;
        gap: 2px;
        min-width: 0;
        white-space: normal;
      }
      .fund-option strong {
        font-size: 12px;
      }
      .fund-option > span,
      .fund-picker-footer {
        font-size: 11px;
        color: var(--p-text-muted-color);
      }
      .fund-picker-footer {
        padding: 8px 12px;
        border-top: 1px solid var(--p-content-border-color);
      }
    `,
  ],
})
export class FundPicker {
  readonly inputId = input.required<string>();
  readonly instruments = input.required<InstrumentChoiceDto[]>();
  readonly value = input.required<string[]>();
  readonly valueChange = output<string[]>();
  readonly compact = input(false);
  readonly overlayContainer = input<HTMLElement | 'body'>('body');
  protected readonly groups = computed(() => {
    const groups = new Map<string, InstrumentChoiceDto[]>();
    for (const item of this.instruments().filter((item) => !item.isVolatilityIndex)) {
      const label = item.group ?? translate<string>('builder.fundSelection.others');
      groups.set(label, [...(groups.get(label) ?? []), item]);
    }
    return [...groups]
      .sort(([a], [b]) => a.localeCompare(b, activeLocale))
      .map(([label, items]) => ({ label, items }));
  });
  protected selectedIn(group: FundGroup): number {
    return group.items.filter((item) => this.value().includes(item.id)).length;
  }
  protected groupState(group: FundGroup): 'true' | 'mixed' | 'false' {
    const selected = this.selectedIn(group);
    return selected === group.items.length ? 'true' : selected ? 'mixed' : 'false';
  }
  protected exceedsLimit(group: FundGroup): boolean {
    return this.value().length + group.items.length - this.selectedIn(group) > 60;
  }
  protected toggleGroup(event: MouseEvent, group: FundGroup): void {
    event.stopPropagation();
    if (this.exceedsLimit(group)) return;
    const ids = new Set(group.items.map((item) => item.id));
    this.valueChange.emit(
      this.groupState(group) === 'true'
        ? this.value().filter((id) => !ids.has(id))
        : [...new Set([...this.value(), ...ids])],
    );
  }
}
