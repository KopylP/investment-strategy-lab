import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { GraphDto, JsonValue, NodeDto } from '../../models/draft-dto';
import { InstrumentChoiceDto } from '../../models/instrument-dto';
import { VocabularyDto, VocabularyFieldDto } from '../../models/vocabulary-dto';

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
  imports: [FormsModule, Button, InputNumber, InputText, Select],
  templateUrl: './inspector.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inspector {
  readonly node = input.required<NodeDto>();
  readonly graph = input.required<GraphDto>();
  readonly vocabulary = input.required<VocabularyDto>();
  readonly instruments = input.required<InstrumentChoiceDto[]>();
  readonly nodeChange = output<NodeDto>();
  readonly duplicate = output<void>();
  readonly allowDuplicate = input(true);

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
    const declared = this.definition()?.fields ?? [];
    if (!this.node().kind.endsWith('Result')) return declared;

    return Object.values(this.vocabulary().blockFields).filter(
      (field) => field.required || Object.hasOwn(this.node().parameters, field.name),
    );
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

  protected fieldValue(field: VocabularyFieldDto): JsonValue | undefined {
    const value = this.node().parameters[field.name];
    if (field.kind === 'Fraction' && typeof value === 'number') return value * 100;
    if (field.kind !== 'Series' || !value || typeof value !== 'object' || Array.isArray(value))
      return value;

    const series = value['series'];
    return series === 'extra' ? `extra:${value['instrumentId']}` : series;
  }

  protected choiceOptions(field: VocabularyFieldDto): { label: string; value: string }[] {
    return Object.entries(field.choiceLabels).map(([value, label]) => ({ label, value }));
  }

  protected changeField(field: VocabularyFieldDto, value: JsonValue): void {
    let stored = value;
    if (field.kind === 'Fraction' && typeof value === 'number') stored = value / 100;
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
    return this.definition()?.label ?? this.node().kind;
  }
}
