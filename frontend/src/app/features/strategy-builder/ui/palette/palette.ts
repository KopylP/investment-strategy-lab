import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JsonValue, NodeKind } from '../../models/draft-dto';
import {
  ConditionKindDto,
  IndicatorKindDto,
  VocabularyFieldDto,
} from '../../models/vocabulary-dto';

export interface NewPaletteNode {
  block: 'Shared';
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
  imports: [FormsModule],
  template: `
    <div class="palette-content">
      <strong>Елементи стратегії</strong>
      <p class="hint">Перетягніть елемент на полотно або натисніть, щоб додати.</p>
      <input
        class="search"
        type="search"
        placeholder="Знайти елемент…"
        aria-label="Пошук елементів"
        [ngModel]="search()"
        (ngModelChange)="search.set($event)"
      />
      <h3>Індикатори</h3>
      @for (indicator of indicators(); track indicator.code) {
        @if (matches(indicator.label)) {
          <button
            type="button"
            draggable="true"
            (dragstart)="drag($event, indicatorRequest(indicator))"
            (click)="addIndicator(indicator)"
            class="item indicator"
          >
            <i class="pi pi-chart-line"></i
            ><span>{{ indicator.label }}<small>Числове значення</small></span
            ><i class="pi pi-plus plus"></i>
          </button>
        }
      }
      <h3>Умови та логіка</h3>
      @for (condition of conditions(); track condition.code) {
        @if (matches(condition.label)) {
          <button
            type="button"
            draggable="true"
            (dragstart)="drag($event, conditionRequest(condition))"
            (click)="addCondition(condition)"
            class="item"
            [class.logic]="['all', 'any', 'not'].includes(condition.code)"
            [class.signal]="condition.code === 'signal'"
          >
            <i
              [class]="
                condition.code === 'signal'
                  ? 'pi pi-bolt'
                  : ['all', 'any', 'not'].includes(condition.code)
                    ? 'pi pi-sitemap'
                    : 'pi pi-filter'
              "
            ></i>
            <span
              >{{ condition.code === 'signal' ? 'Мій сигнал' : condition.label
              }}<small>{{
                ['all', 'any', 'not'].includes(condition.code) ? 'Поєднати умови' : 'Так / ні'
              }}</small></span
            ><i class="pi pi-plus plus"></i>
          </button>
        }
      }
      @if (matches('Відібрати фонди')) {
        <h3>Модулі відбору</h3>
        <button
          class="item"
          type="button"
          draggable="true"
          (dragstart)="drag($event, selectorRequest)"
          (click)="add.emit(selectorRequest)"
        >
          <i class="pi pi-filter"></i><span>Відібрати фонди<small>Оцінка → кандидати</small></span
          ><i class="pi pi-plus plus"></i>
        </button>
      }
      @if (
        !indicators().some(matchesItem) &&
        !conditions().some(matchesItem) &&
        !matches('Відібрати фонди')
      ) {
        <p class="hint">Елементів не знайдено.</p>
      }
      <p class="hint">
        Один вузол можна підключити до кількох правил. Гілки завершуються вузлами результату.
      </p>
    </div>
  `,
  styles: [
    `
      .palette-content {
        padding: 12px 10px;
      }
      strong {
        font-size: 14px;
      }
      .hint {
        font-size: 12px;
        color: var(--p-text-muted-color);
        line-height: 1.5;
        margin: 8px 0 16px;
      }
      .search {
        width: 100%;
        border: 1px solid var(--p-content-border-color);
        border-radius: 6px;
        padding: 9px;
        margin-top: 14px;
        font-size: 12px;
        background: var(--p-content-background);
        color: var(--p-text-color);
      }
      h3 {
        font-size: 12px;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: var(--p-text-muted-color);
        margin: 16px 0 8px;
      }
      .item {
        display: flex;
        align-items: center;
        gap: 8px;
        width: 100%;
        text-align: left;
        padding: 8px;
        margin: 6px 0;
        border: 1px solid var(--p-content-border-color);

        border-radius: 6px;
        background: var(--p-content-background);
        color: var(--p-text-color);
        cursor: grab;
        font-size: 12px;
      }
      .item:hover,
      .item:focus-visible {
        background: var(--p-content-hover-background);
        color: var(--p-content-hover-color);
      }
      .item:hover small,
      .item:focus-visible small {
        color: var(--p-text-hover-muted-color);
      }
      .item:focus-visible {
        outline: 2px solid var(--p-primary-color);
      }
      .item.logic {
        border-radius: 6px;
      }
      small {
        display: block;
        font-size: 12px;
        color: var(--p-text-muted-color);
        margin-top: 3px;
      }
      .plus {
        margin-left: auto;
        font-size: 12px;
      }
      @media (min-width: 1600px) {
        .palette-content {
          padding: 16px 14px;
        }
        strong {
          font-size: 15px;
        }
        .hint,
        .search,
        .item {
          font-size: 13px;
        }
        .item {
          padding: 10px;
          gap: 10px;
        }
        .item small,
        h3 {
          font-size: 12px;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Palette {
  readonly overlayContainer = input<HTMLElement | 'body'>('body');
  readonly indicators = input.required<IndicatorKindDto[]>();
  readonly conditions = input.required<ConditionKindDto[]>();
  readonly add = output<NewPaletteNode>();
  protected readonly selectorRequest: NewPaletteNode = {
    kind: 'FundSelection',
    block: 'Shared',
    parameters: { top: 3, order: 'desc' },
  };
  protected readonly search = signal('');
  protected matches(label: string): boolean {
    return label.toLocaleLowerCase().includes(this.search().trim().toLocaleLowerCase());
  }
  protected readonly matchesItem = (item: { label: string }) => this.matches(item.label);
  protected indicatorRequest(indicator: IndicatorKindDto): NewPaletteNode {
    return {
      block: 'Shared',
      kind: 'Indicator',
      parameters: { indicator: indicator.code, ...defaultsOf(indicator.fields) },
    };
  }
  protected conditionRequest(condition: ConditionKindDto): NewPaletteNode {
    return {
      block: 'Shared',
      kind: conditionKinds[condition.code],
      parameters: defaultsOf(condition.fields),
    };
  }
  protected drag(event: DragEvent, node: NewPaletteNode) {
    event.dataTransfer?.setData('application/strategy-node', JSON.stringify(node));
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'copy';
  }
  protected addIndicator(indicator: IndicatorKindDto): void {
    this.add.emit(this.indicatorRequest(indicator));
  }
  protected addCondition(condition: ConditionKindDto): void {
    this.add.emit(this.conditionRequest(condition));
  }
}

function defaultsOf(fields: VocabularyFieldDto[]): Record<string, JsonValue> {
  return Object.fromEntries(
    fields
      .filter((field) => field.default !== null || field.name === 'of')
      .map((field) => [field.name, field.name === 'of' ? { series: 'fund' } : field.default]),
  );
}
