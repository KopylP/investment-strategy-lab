import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { OwnSignalDto } from '../../models/own-signal-dto';
import { blocks } from '../../models/draft-vm';
import { GraphDto, JsonValue, NodeDto } from '../../models/draft-dto';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { VocabularyDto, VocabularyFieldDto } from '../../models/vocabulary-dto';
import { fitNodeSelectOverlay } from '../graph/node-select-overlay';
import { GateOutcome } from './gate-outcome';
import { fieldHints, isLossLimit, resultFieldGroups } from './result-fields';

const conditionCodes: Record<string, string> = {
  Compare: 'compare',
  Between: 'between',
  Crossed: 'crossed',
  Signal: 'signal',
  All: 'all',
  Any: 'any',
  Not: 'not',
};

@Component({
  selector: 'app-node-inspector',
  imports: [NgTemplateOutlet, FormsModule, Button, InputNumber, InputText, Select, GateOutcome],
  templateUrl: './inspector.html',
  styles: [
    `
      :host {
        display: block;
        min-width: 0;
      }
      .inline-fields {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 8px;
      }
      .inline-fields .wide,
      .inline-fields > label {
        grid-column: 1 / -1;
      }
      .inline-fields label {
        font-size: 12px;
        font-weight: 500;
      }
      .inline-fields .parameter-field {
        min-width: 0;
        gap: 4px;
      }
      .inline-inspector {
        --p-form-field-padding-x: 8px;
        --p-form-field-padding-y: 5px;
        --p-form-field-sm-font-size: 12px;
      }
      .field-section + .field-section {
        margin-top: 10px;
      }
      .group-title {
        margin: 0 0 7px;
        font-size: 12px;
        font-weight: 600;
      }
      .advanced-fields {
        margin-top: 10px;
        border-top: 1px solid var(--p-content-border-color);
        padding-top: 8px;
        font-size: 12px;
      }
      .advanced-fields summary {
        cursor: pointer;
        color: var(--p-text-muted-color);
      }
      .advanced-fields summary:focus-visible {
        outline: 2px solid var(--p-primary-color);
      }
      .advanced-fields[open] summary {
        margin-bottom: 8px;
      }
      .field-hint {
        color: var(--p-text-muted-color);
        font-size: 12px;
        line-height: 1.4;
      }
      .budget-preview {
        margin-top: 8px;
        font-size: 12px;
      }
      .budget-bar {
        display: flex;
        height: 8px;
        border-radius: 4px;
        overflow: hidden;
        background: var(--p-content-border-color);
      }
      .invested {
        background: var(--p-primary-color);
      }
      .budget-labels,
      .selection-flow {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 5px;
        margin-top: 5px;
        color: var(--p-text-muted-color);
      }
      .selection-flow {
        justify-content: flex-start;
        margin: 0 0 10px;
        font-size: 12px;
        flex-wrap: wrap;
      }
      .selection-flow span {
        padding: 3px 5px;
        border-radius: 4px;
        border: 1px solid var(--p-content-border-color);
        color: var(--p-text-color);
      }
      .selection-flow i {
        font-size: 12px;
      }
      .risk-state {
        margin-bottom: 10px;
        padding: 8px;
        border-radius: 6px;
        background: var(--p-content-hover-background);
        font-size: 12px;
        line-height: 1.45;
      }
      .risk-state p {
        margin: 4px 0 0;
        color: var(--p-text-muted-color);
        font-size: 12px;
      }
      .market-pass {
        display: flex;
        align-items: center;
        gap: 5px;
        margin: 0 0 8px;
        font-size: 12px;
        color: var(--p-text-muted-color);
      }
      .market-pass i {
        font-size: 12px;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inspector {
  protected readonly fitNodeSelect = fitNodeSelectOverlay;
  readonly node = input.required<NodeDto>();
  readonly graph = input.required<GraphDto>();
  readonly vocabulary = input.required<VocabularyDto>();
  readonly instruments = input.required<InstrumentChoiceDto[]>();
  readonly overlayContainer = input<HTMLElement | 'body'>('body');
  readonly signals = input<OwnSignalDto[]>([]);
  protected readonly marketSignals = computed(() =>
    this.signals().filter((signal) => signal.isMarket),
  );
  readonly nodeChange = output<NodeDto>();
  readonly duplicate = output<void>();
  readonly remove = output<void>();
  readonly allowDuplicate = input(true);
  readonly guided = input(false);
  readonly inline = input(false);

  protected readonly definition = computed(() => {
    if (this.node().kind === 'Indicator') {
      const code = this.node().parameters['indicator'];
      return this.vocabulary().indicators.find((indicator) => indicator.code === code);
    }

    return this.vocabulary().conditions.find(
      (condition) => condition.code === conditionCodes[this.node().kind],
    );
  });
  protected readonly fields = computed(() => {
    const declared = (this.definition()?.fields ?? []).filter(
      (field) =>
        !(
          field.kind === 'Series' &&
          this.graph().links.some((link) => link.to === this.node().id && link.input === field.name)
        ),
    );
    if (this.node().kind === 'Signal') return declared.filter((field) => field.name !== 'signalId');
    if (!this.node().kind.endsWith('Result') && this.node().kind !== 'FundSelection')
      return declared;

    return resultFieldGroups(this.node(), this.vocabulary()).flatMap((group) => group.fields);
  });
  protected readonly fieldGroups = computed(() =>
    this.node().kind.endsWith('Result') || this.node().kind === 'FundSelection'
      ? resultFieldGroups(this.node(), this.vocabulary())
      : [{ title: '', fields: this.fields(), advanced: false }],
  );
  protected readonly fieldHints = fieldHints;
  protected readonly activeRiskLimits = computed(
    () =>
      ['stopLossFromCost', 'drawdownFromPeak'].filter(
        (name) => typeof this.node().parameters[name] === 'number',
      ).length,
  );
  protected readonly marketShare = computed(() => {
    const value = this.node().parameters['investWhenUnfavorable'];
    return typeof value === 'number' ? Math.round(value * 10000) / 100 : null;
  });
  protected readonly links = computed(() => [
    ...this.graph()
      .links.filter((link) => link.to === this.node().id)
      .map((link) => `${link.input} ← ${link.from}`),
    ...this.graph()
      .links.filter((link) => link.from === this.node().id)
      .map((link) => `${link.to} · ${link.input}`),
  ]);
  protected readonly seriesOptions = computed(() => [
    { label: 'Цей фонд', value: 'fund' },
    { label: 'Бенчмарк', value: 'benchmark' },
    { label: 'Фонд для кешу', value: 'cash' },
    { label: 'Індекс волатильності', value: 'volatilityIndex' },
    ...this.instruments().map((instrument) => ({
      label: instrument.label,
      value: `extra:${instrument.id}`,
    })),
  ]);

  protected changeSignal(id: string): void {
    this.nodeChange.emit({
      ...this.node(),
      parameters: { ...this.node().parameters, signalId: id },
    });
  }

  protected fieldValue(field: VocabularyFieldDto): JsonValue | undefined {
    const value = this.node().parameters[field.name];
    if (field.kind === 'Fraction' && typeof value === 'number')
      return (isLossLimit(field.name) ? Math.abs(value) : value) * 100;
    if (field.kind !== 'Series' || !value || typeof value !== 'object' || Array.isArray(value))
      return value;

    const series = value['series'];
    return series === 'extra' ? `extra:${value['instrumentId']}` : series;
  }

  protected choiceOptions(field: VocabularyFieldDto): { label: string; value: string }[] {
    if (field.name === 'order')
      return [
        { label: 'Найвищі спочатку', value: 'desc' },
        { label: 'Найнижчі спочатку', value: 'asc' },
      ];
    if (field.name === 'method')
      return [
        { label: 'Порівну', value: 'equal' },
        { label: 'Більше менш мінливим фондам', value: 'inverseVolatility' },
      ];
    return Object.entries(field.choiceLabels).map(([value, label]) => ({ label, value }));
  }

  protected fractionMinimum(field: VocabularyFieldDto): number | undefined {
    return isLossLimit(field.name) ? 0 : field.minimum === null ? undefined : field.minimum * 100;
  }
  protected fractionMaximum(field: VocabularyFieldDto): number | undefined {
    return isLossLimit(field.name) ? 100 : field.maximum === null ? undefined : field.maximum * 100;
  }
  protected placeholder(field: VocabularyFieldDto): string | undefined {
    return !field.required
      ? isLossLimit(field.name)
        ? 'Вкажіть відсоток'
        : 'Без обмеження'
      : undefined;
  }

  protected changeField(field: VocabularyFieldDto, value: JsonValue): void {
    let stored = value;
    if (field.kind === 'Fraction' && typeof value === 'number')
      stored = (isLossLimit(field.name) ? -Math.abs(value) : value) / 100;
    if (field.kind === 'Series' && typeof value === 'string') {
      stored = value.startsWith('extra:')
        ? { series: 'extra', instrumentId: value.slice('extra:'.length) }
        : { series: value };
    }

    this.nodeChange.emit({
      ...this.node(),
      parameters: { ...this.node().parameters, [field.name]: stored },
    });
  }

  protected title(): string {
    if (this.node().kind === 'FundSelection') return 'Відібрати фонди';
    return (
      this.definition()?.label ??
      (this.node().kind.endsWith('Result')
        ? blocks.find((block) => block.kind === this.node().block)?.result
        : null) ??
      this.node().kind
    );
  }
}
