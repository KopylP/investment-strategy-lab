import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { BlockKind, JsonValue, NodeKind } from '../../models/draft-dto';
import { blocks } from '../../models/draft-vm';
import {
  ConditionKindDto,
  IndicatorKindDto,
  VocabularyFieldDto,
} from '../../models/vocabulary-dto';

export interface NewPaletteNode {
  block: BlockKind;
  kind: NodeKind;
  parameters: Record<string, JsonValue>;
}

const conditionKinds: Record<string, NodeKind> = {
  compare: 'Compare',
  between: 'Between',
  crossed: 'Crossed',
  signal: 'Signal',
  all: 'All',
  any: 'Any',
  not: 'Not',
};

@Component({
  selector: 'app-node-palette',
  imports: [FormsModule, Button, Select],
  template: `
    <div class="card mb-3 flex flex-wrap items-center gap-2 py-3">
      <span class="mr-1 text-sm font-medium">Додати блок</span>
      <p-select
        [options]="blockOptions"
        optionLabel="label"
        optionValue="value"
        [ngModel]="block()"
        (ngModelChange)="block.set($event)"
        size="small"
      />
      @for (indicator of indicators(); track indicator.code) {
        <p-button
          [label]="indicator.label"
          icon="pi pi-chart-line"
          size="small"
          severity="secondary"
          [outlined]="true"
          (onClick)="addIndicator(indicator)"
        />
      }
      @for (condition of conditions(); track condition.code) {
        <p-button
          [label]="condition.label"
          icon="pi pi-directions-alt"
          size="small"
          severity="secondary"
          [outlined]="true"
          (onClick)="addCondition(condition)"
        />
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Palette {
  readonly indicators = input.required<IndicatorKindDto[]>();
  readonly conditions = input.required<ConditionKindDto[]>();
  readonly add = output<NewPaletteNode>();
  protected readonly block = signal<BlockKind>('Entry');
  protected readonly blockOptions = blocks.map((block) => ({
    label: `${block.number}. ${block.title}`,
    value: block.kind,
  }));

  protected addIndicator(indicator: IndicatorKindDto): void {
    this.add.emit({
      block: this.block(),
      kind: 'Indicator',
      parameters: { indicator: indicator.code, ...defaultsOf(indicator.fields) },
    });
  }

  protected addCondition(condition: ConditionKindDto): void {
    this.add.emit({
      block: this.block(),
      kind: conditionKinds[condition.code],
      parameters: defaultsOf(condition.fields),
    });
  }
}

function defaultsOf(fields: VocabularyFieldDto[]): Record<string, JsonValue> {
  return Object.fromEntries(
    fields
      .filter((field) => field.default !== null || field.name === 'of')
      .map((field) => [field.name, field.name === 'of' ? { series: 'fund' } : field.default]),
  );
}
