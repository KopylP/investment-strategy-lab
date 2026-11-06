import { Component, Input, OnChanges, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { KeyValuePipe } from '@angular/common';
import { RefDirective } from 'rete-angular-plugin/21';
import { StrategyNode } from './rete-model';
import { DraftSetupDto } from '../../models/draft-dto';
import { setupFields } from '../../data-access/setup-bindings';
import { FundSelectionDialog } from '../fund-selection-dialog/fund-selection-dialog';
import { setupPickers } from '../../data-access/setup-pickers';
import { InstrumentPickerVm } from '../../models/instrument-picker-vm';

@Component({
  selector: 'app-setup-rete-node',
  imports: [FormsModule, KeyValuePipe, RefDirective, FundSelectionDialog],
  host: { '[style.width.px]': 'data.width' },
  template: `
    <article
      data-node-id="strategy-setup"
      [style.opacity]="data.branchDimmed ? 0.35 : 1"
      (click)="data.choose()"
      data-node-category="Налаштування"
      tabindex="0"
      aria-label="Фонди й ряди · постійний вузол"
    >
      <header>
        <i class="pi pi-sliders-h"></i><strong>Фонди й ряди</strong
        ><i class="pi pi-lock" title="Постійний вузол" aria-label="Постійний вузол"></i>
      </header>
      <div
        class="fields"
        (pointerdown)="$event.stopPropagation()"
        (dblclick)="$event.stopPropagation()"
        (wheel)="$event.stopPropagation()"
      >
        @for (field of fields; track field.key) {
          <div class="field">
            <label [for]="'setup-node-' + field.key">{{ field.label }}</label>
            <div class="field-value">
              <button
                type="button"
                class="picker-button"
                [class.empty]="!picked(field.key).length"
                [class.required]="pickerOf(field.key).required"
                (click)="openPicker(field.key)"
                aria-haspopup="dialog"
                [attr.aria-label]="field.label + ' · ' + pickerSummary(field.key)"
              >
                <i
                  class="pi"
                  [class]="field.key === 'funds' ? 'pi-folder-open' : 'pi-chart-line'"
                ></i>
                <span class="picker-label">{{ pickerSummary(field.key) }}</span>
                @if (pickerOf(field.key).limit > 1 && picked(field.key).length) {
                  <span class="picker-count">{{ picked(field.key).length }}</span>
                }
                <i class="pi pi-chevron-right"></i>
              </button>
              <button
                type="button"
                class="port"
                [attr.aria-label]="'Вихід ' + field.label"
                [title]="
                  field.key === 'funds' ? 'Вибрані фонди для правил та індикаторів' : field.label
                "
                [attr.data-socket-key]="data.id + ':output:' + field.key"
                (click)="keyboard($event, field.key)"
                refComponent
                [data]="{
                  type: 'socket',
                  side: 'output',
                  key: field.key,
                  nodeId: data.id,
                  payload: data.outputs[field.key]?.socket,
                  seed,
                }"
                [emit]="emit"
              ></button>
            </div>
            @if (field.key === 'funds') {
              <div class="fund-loop" aria-label="Вибрані фонди перебираються по одному">
                <div class="loop-heading">
                  <i class="pi pi-sync"></i><span>Для кожного фонду</span
                  ><b>×{{ data.setup.funds.length }}</b>
                </div>
                <div class="loop-items">
                  @for (fund of selectedFunds.slice(0, 3); track fund.id; let last = $last) {
                    <span class="fund-token" [title]="fund.name">{{ fund.label }}</span>
                    @if (!last) {
                      <i class="pi pi-angle-right"></i>
                    }
                  }
                  @if (selectedFunds.length > 3) {
                    <span class="more-funds">+{{ selectedFunds.length - 3 }}</span>
                  }
                  @if (!selectedFunds.length) {
                    <span class="more-funds">Оберіть фонди для перебору</span>
                  }
                </div>
              </div>
            }
          </div>
        }
        @for (output of data.outputs | keyvalue; track output.key) {
          @if (output.key.startsWith('extra:')) {
            <div class="extra-row">
              <span>{{ extraLabel(output.key) }}</span>
              <button
                class="port"
                type="button"
                [attr.aria-label]="'Вихід ' + extraLabel(output.key)"
                [attr.data-socket-key]="data.id + ':output:' + output.key"
                (click)="keyboard($event, output.key)"
                refComponent
                [data]="{
                  type: 'socket',
                  side: 'output',
                  key: output.key,
                  nodeId: data.id,
                  payload: output.value?.socket,
                  seed,
                }"
                [emit]="emit"
              ></button>
            </div>
          }
        }
      </div>
    </article>
    <app-fund-selection-dialog
      [(visible)]="pickerVisible"
      [picker]="pickerOf(pickerKey())"
      [overlayContainer]="data.overlayContainer"
      [instruments]="data.instruments"
      [value]="picked(pickerKey())"
      (applied)="applyPicked($event)"
      (instrumentsChanged)="data.refreshInstruments()"
    />
  `,
  styles: [
    `
      :host {
        display: block;
        width: 320px;
      }
      article {
        background: var(--p-content-background);
        color: var(--p-text-color);
        border: 1px solid var(--p-content-border-color);
        border-radius: 10px;
        box-shadow: 0 4px 16px #0000000d;
      }
      header {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 12px;
        border-bottom: 1px solid var(--p-content-border-color);
        font-size: 13px;
        cursor: grab;
      }
      header .pi-lock {
        margin-left: auto;
        font-size: 11px;
        color: var(--p-text-muted-color);
      }
      .fields {
        padding: 10px 0 10px 12px;
        display: flex;
        flex-direction: column;
        gap: 10px;
        cursor: default;
        --p-form-field-padding-x: 8px;
        --p-form-field-padding-y: 5px;
        --p-form-field-sm-font-size: 12px;
      }
      .field {
        min-width: 0;
      }
      label {
        font-size: 11px;
        font-weight: 500;
        display: block;
        margin-bottom: 4px;
      }
      .field-value {
        display: flex;
        align-items: center;
        gap: 6px;
      }
      .picker-button {
        flex: 1;
        min-width: 0;
        display: flex;
        align-items: center;
        gap: 8px;
        text-align: left;
        padding: 9px 10px;
        border: 1px solid var(--p-primary-color);
        border-radius: 8px;
        background: color-mix(in srgb, var(--p-primary-color) 14%, transparent);
        color: var(--p-text-color);
        cursor: pointer;
        font-size: 13px;
        font-weight: 600;
        transition:
          background 0.15s,
          box-shadow 0.15s;
      }
      .picker-button > .pi:first-child {
        color: var(--p-primary-color);
        font-size: 14px;
      }
      .picker-button .pi-chevron-right {
        margin-left: auto;
        font-size: 11px;
        color: var(--p-primary-color);
      }
      .picker-label {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .picker-count {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        box-sizing: border-box;
        flex-shrink: 0;
        min-width: 20px;
        height: 20px;
        padding: 0 5px;
        border-radius: 10px;
        background: var(--p-primary-color);
        color: var(--p-primary-contrast-color);
        font-size: 11px;
        line-height: 1;
        font-variant-numeric: tabular-nums;
      }
      .picker-button:hover {
        background: color-mix(in srgb, var(--p-primary-color) 24%, transparent);
        box-shadow: 0 0 0 3px color-mix(in srgb, var(--p-primary-color) 18%, transparent);
      }
      .picker-button:focus-visible {
        outline: 2px solid var(--p-primary-color);
        outline-offset: 2px;
      }
      .picker-button.empty:not(.required) {
        border-style: dashed;
        background: transparent;
        color: var(--p-text-muted-color);
        font-weight: 500;
      }
      .picker-button.empty.required {
        background: var(--p-primary-color);
        color: var(--p-primary-contrast-color);
        animation: picker-pulse 2s ease-in-out infinite;
      }
      .picker-button.empty.required .pi {
        color: var(--p-primary-contrast-color);
      }
      @keyframes picker-pulse {
        0%,
        100% {
          box-shadow: 0 0 0 0 color-mix(in srgb, var(--p-primary-color) 45%, transparent);
        }
        50% {
          box-shadow: 0 0 0 6px color-mix(in srgb, var(--p-primary-color) 0%, transparent);
        }
      }
      @media (prefers-reduced-motion: reduce) {
        .picker-button.empty.required {
          animation: none;
        }
      }
      .port {
        display: grid;
        place-items: center;
        width: 28px;
        height: 28px;
        flex-shrink: 0;
        border: 0;
        padding: 0;
        background: transparent;
        border-radius: 50%;
        cursor: crosshair;
      }
      .port:focus-visible,
      .find:focus-visible {
        outline: 2px solid var(--p-primary-color);
      }
      .find {
        font-size: 11px;
        display: flex;
        align-items: center;
        gap: 5px;
        margin-top: 5px;
        padding: 4px 6px;
        border-radius: 4px;
        cursor: pointer;
        color: var(--p-text-muted-color);
      }
      .find:hover {
        color: var(--p-content-hover-color);
        background: var(--p-content-hover-background);
      }
      .extra-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        font-size: 11px;
        gap: 6px;
      }
      .fund-loop {
        margin: 7px 28px 0 0;
        padding: 7px 8px;
        border: 1px solid var(--p-content-border-color);
        border-radius: 7px;
        background: var(--p-content-hover-background);
      }
      .loop-heading,
      .loop-items {
        display: flex;
        align-items: center;
        gap: 5px;
        font-size: 10px;
      }
      .loop-heading {
        color: var(--p-text-color);
      }
      .loop-heading .pi {
        font-size: 11px;
      }
      .loop-heading b {
        margin-left: auto;
        font-variant-numeric: tabular-nums;
      }
      .loop-items {
        margin-top: 6px;
        color: var(--p-text-muted-color);
      }
      .fund-token {
        max-width: 66px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        padding: 2px 5px;
        border: 1px solid var(--p-content-border-color);
        border-radius: 4px;
        background: var(--p-content-background);
        color: var(--p-text-color);
      }
      .loop-items .pi {
        font-size: 10px;
      }
      .more-funds {
        white-space: nowrap;
      }
    `,
  ],
})
export class SetupNode implements OnChanges {
  @Input() data!: StrategyNode;
  @Input() emit!: (event: any) => void;
  @Input() rendered!: () => void;
  protected readonly fields = setupFields.filter((field) => field.key !== 'cashId');
  protected readonly pickerVisible = signal(false);
  protected readonly pickerKey = signal('funds');
  seed = 0;
  get selectedFunds() {
    return this.data.setup.funds.map(
      (id) =>
        this.data.instruments.find((item) => item.id === id) ?? { id, label: 'Фонд', name: '' },
    );
  }
  pickerOf(key: string): InstrumentPickerVm {
    return setupPickers[key];
  }
  picked(key: string): string[] {
    const value = this.data.setup[key as keyof DraftSetupDto];
    if (Array.isArray(value)) return value;

    return typeof value === 'string' ? [value] : [];
  }
  pickerSummary(key: string): string {
    const picker = this.pickerOf(key);
    const ids = this.picked(key);
    if (!ids.length) return picker.chooseLabel;
    if (picker.limit > 1) return picker.changeLabel;

    return this.data.instruments.find((item) => item.id === ids[0])?.label ?? picker.changeLabel;
  }
  openPicker(key: string) {
    this.pickerKey.set(key);
    this.pickerVisible.set(true);
  }
  applyPicked(ids: string[]) {
    const key = this.pickerKey();
    const value = this.pickerOf(key).limit > 1 ? ids : (ids[0] ?? null);
    this.patch(key, value);
  }
  patch(key: string, value: string[] | string | null) {
    this.data.setup = { ...this.data.setup, [key]: value };
    this.data.changeSetup(this.data.setup);
  }
  extraLabel(key: string) {
    return (
      this.data.instruments.find((item) => item.id === key.slice(6))?.label ?? 'Додатковий ряд'
    );
  }
  keyboard(event: MouseEvent, key: string) {
    if (event.detail === 0) this.data.outputClick(key);
    event.stopPropagation();
  }
  ngOnChanges() {
    this.seed++;
    requestAnimationFrame(() => this.rendered?.());
  }
}
