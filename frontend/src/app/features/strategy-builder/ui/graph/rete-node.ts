import { Component, Input, OnChanges } from '@angular/core';
import { KeyValuePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Select } from 'primeng/select';
import { RefDirective } from 'rete-angular-plugin/21';
import { Inspector } from '../inspector/inspector';
import { StrategyNode } from './rete-model';
import { nodeStyle } from './graph-visuals';
import { blocks } from '../../models/draft-vm';
import { resultsUsing } from '../../data-access/shared-graph';
import { fitNodeSelectOverlay } from './node-select-overlay';
import { evaluationScope } from '../../data-access/evaluation-scope';

@Component({
  selector: 'app-strategy-rete-node',
  imports: [RefDirective, KeyValuePipe, Inspector, FormsModule, Select],
  host: { '[style.width.px]': 'data.width' },
  template: `
    <article
      tabindex="0"
      role="group"
      [attr.aria-label]="style.label + ' · ' + role"
      [attr.data-node-id]="data.id"
      [attr.data-node-category]="style.label"
      [class.selected]="data.selected"
      [style.opacity]="data.branchDimmed ? 0.35 : 1"
      [style.--node-color]="style.color"
      [class.logic]="['All', 'Any', 'Not'].includes(data.dto.kind)"
      [class.result]="data.dto.kind.endsWith('Result')"
      [class.multi-fund]="scope === 'per-fund' && data.setup.funds.length > 1"
      [attr.data-evaluation-scope]="scope"
      [attr.data-iteration-count]="
        scope === 'per-fund' && data.dto.kind !== 'RiskResult' ? data.setup.funds.length : null
      "
      (click)="data.choose()"
    >
      <header [title]="role">
        <i [class]="'pi ' + style.icon"></i><strong>{{ title }}</strong>
        <span class="role">{{ shortRole }}</span>
        @if (!data.dto.kind.endsWith('Result') && !['Cash', 'Portfolio'].includes(data.dto.kind)) {
          <div
            class="node-actions"
            (pointerdown)="$event.stopPropagation()"
            (click)="$event.stopPropagation()"
          >
            <button
              type="button"
              aria-label="Дублювати"
              title="Дублювати"
              (click)="data.duplicate()"
            >
              <i class="pi pi-copy"></i>
            </button>
            <button
              type="button"
              aria-label="Видалити вузол"
              title="Видалити вузол"
              (click)="data.remove()"
            >
              <i class="pi pi-trash"></i>
            </button>
          </div>
        }
      </header>
      <div class="evaluation-scope" [attr.title]="scopeTitle">
        <i
          [class]="
            scope === 'per-fund'
              ? 'pi pi-sync'
              : scope === 'portfolio'
                ? 'pi pi-th-large'
                : 'pi pi-globe'
          "
          aria-hidden="true"
        ></i>
        <span>{{
          scope === 'per-fund'
            ? data.dto.kind === 'RiskResult'
              ? 'Кожна утримувана позиція'
              : 'Кожен фонд'
            : scope === 'portfolio'
              ? portfolioScopeLabel
              : 'Спільне для всіх'
        }}</span>
        @if (scope === 'per-fund' && data.dto.kind !== 'RiskResult') {
          <b>×{{ data.setup.funds.length }}</b>
        } @else if (scope === 'portfolio') {
          <span class="collect" aria-hidden="true">⋯ → <i class="pi pi-th-large"></i></span>
        }
      </div>
      @if (data.problem; as problem) {
        <p role="alert" class="problem">{{ problem.message }}</p>
      }
      <div class="ports">
        <div class="port-column inputs">
          @for (input of data.inputs | keyvalue: inputOrder; track input.key) {
            <div
              class="port-row"
              [class.dimmed]="data.connecting && !data.compatible.includes(input.key)"
            >
              <button
                type="button"
                class="port"
                [class.compatible]="data.connecting && data.compatible.includes(input.key)"
                [attr.aria-label]="'Вхід ' + input.key"
                [attr.data-socket-key]="data.id + ':input:' + input.key"
                (click)="ifKeyboardInput($event, input.key); $event.stopPropagation()"
                refComponent
                [data]="{
                  type: 'socket',
                  side: 'input',
                  key: input.key,
                  nodeId: data.id,
                  payload: input.value?.socket,
                  seed,
                }"
                [emit]="emit"
              ></button
              ><span>{{ inputLabel(input.key) }}</span>
            </div>
          }
        </div>
        <div class="port-column outputs">
          @for (output of data.outputs | keyvalue: outputOrder; track output.key) {
            <div class="port-row">
              <span>{{ outputLabel(output.key) }}</span>
              <button
                type="button"
                class="port"
                [attr.aria-label]="
                  output.key === 'output' ? 'Почати звʼязок' : 'Вихід ' + outputLabel(output.key)
                "
                [attr.data-socket-key]="
                  data.id + ':output' + (output.key === 'output' ? '' : ':' + output.key)
                "
                (click)="ifKeyboardOutput($event, output.key); $event.stopPropagation()"
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
        </div>
      </div>
      <div
        class="parameters"
        (pointerdown)="$event.stopPropagation()"
        (dblclick)="$event.stopPropagation()"
        (wheel)="$event.stopPropagation()"
      >
        @if (data.dto.kind === 'Portfolio') {
          <div class="portfolio-summary" data-portfolio-summary>
            <strong>Фонди + кеш = 100%</strong>
            <p>
              Відібрані фонди отримують розраховані ваги. Невикористана частина бюджету залишається
              в кеші.
            </p>
            <p>Налаштування ризику застосовуються до утримуваних позицій між ребалансуваннями.</p>
          </div>
        } @else if (data.dto.kind === 'Cash') {
          <label class="cash-label" [for]="data.id + '-instrument'">Залишок вкладати у</label>
          <p-select
            #nodeSelect
            panelStyleClass="builder-node-select-panel"
            (onShow)="fitNodeSelect(nodeSelect)"
            [inputId]="data.id + '-instrument'"
            size="small"
            [options]="cashOptions"
            optionLabel="label"
            optionValue="id"
            [ngModel]="data.setup.cashId"
            (ngModelChange)="changeCash($event ?? null)"
            [filter]="true"
            [showClear]="true"
            placeholder="Фонд для кешу"
            [appendTo]="data.overlayContainer"
            fluid
          />
          <p class="cash-hint">Отримує гроші, що залишилися після відбору, обмежень і розподілу.</p>
        } @else {
          <app-node-inspector
            [inline]="true"
            [overlayContainer]="data.overlayContainer"
            [node]="data.dto"
            [graph]="data.graph"
            [vocabulary]="data.vocabulary"
            [signals]="data.signals"
            [instruments]="data.instruments"
            (nodeChange)="data.change($event)"
            (duplicate)="data.duplicate()"
            (remove)="data.remove()"
          />
        }
      </div>
    </article>
  `,
  styles: [
    `
      :host {
        display: block;
        width: 240px;
      }
      article {
        background: var(--p-content-background, white);
        border: 1px solid color-mix(in srgb, var(--node-color) 45%, var(--p-surface-300));

        border-radius: 12px;
        box-shadow: 0 4px 16px #0000000d;
        color: var(--p-text-color);
      }
      article.selected {
        outline: 3px solid color-mix(in srgb, var(--node-color) 35%, transparent);
      }
      article.multi-fund {
        box-shadow:
          0 3px 0 -1px var(--p-content-background),
          0 3px 0 var(--p-content-border-color),
          0 6px 0 -1px var(--p-content-background),
          0 6px 0 var(--p-content-border-color),
          0 8px 16px #0000000d;
      }
      .evaluation-scope {
        display: flex;
        align-items: center;
        gap: 5px;
        margin: 0 8px;
        padding: 5px 0;
        border-bottom: 1px solid var(--p-content-border-color);
        color: var(--p-text-muted-color);
        font-size: 10px;
      }
      .evaluation-scope > i {
        font-size: 10px;
      }
      .evaluation-scope b,
      .collect {
        margin-left: auto;
        font-variant-numeric: tabular-nums;
      }
      .collect i {
        font-size: 9px;
      }
      article.logic {
        border-radius: 16px;
      }
      article.result {
        border-width: 2px;
      }
      header {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 7px 8px;
        background: color-mix(in srgb, var(--node-color) 7%, transparent);
        cursor: grab;
        border-radius: 10px 10px 0 0;
        font-size: 12px;
      }
      header i {
        color: var(--node-color);
      }
      .role {
        margin-left: auto;
        font-size: 10px;
        color: var(--p-text-muted-color);
      }
      .ports {
        display: flex;
        justify-content: space-between;
        gap: 8px;
        padding: 3px 0;
        font-size: 11px;
        color: var(--p-text-muted-color);
      }
      .port-row {
        display: flex;
        align-items: center;
        gap: 6px;
        min-height: 28px;
      }
      .port-column {
        display: flex;
        flex-direction: column;
        min-width: 0;
      }
      .outputs {
        align-items: flex-end;
        flex-shrink: 0;
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
        cursor: crosshair;
        border-radius: 50%;
      }
      .compatible {
        outline: 3px solid #86efac;
      }
      .dimmed {
        opacity: 0.3;
      }
      .parameters {
        padding: 0 8px 8px;
        cursor: default;
      }
      header strong {
        flex: 1;
        min-width: 0;
        line-height: 1.3;
      }
      .node-actions {
        display: flex;
        gap: 2px;
      }
      .node-actions button {
        width: 24px;
        height: 24px;
        border-radius: 4px;
        cursor: pointer;
        color: var(--p-text-muted-color);
      }
      .node-actions button:hover,
      .node-actions button:focus-visible {
        background: var(--p-content-hover-background);
        color: var(--p-content-hover-color);
      }
      .node-actions i {
        font-size: 11px;
        color: inherit;
      }
      .problem {
        margin: 8px 12px;
        font-size: 12px;
        color: var(--p-red-500);
      }
      .portfolio-summary {
        font-size: 11px;
        line-height: 1.5;
        padding: 8px 4px;
      }
      .portfolio-summary strong {
        color: var(--node-color);
      }
      .portfolio-summary p {
        margin: 8px 0 0;
        color: var(--p-text-muted-color);
      }
      .cash-label {
        display: block;
        font-size: 11px;
        margin-bottom: 5px;
      }
      .cash-hint {
        color: var(--p-text-muted-color);
        font-size: 10px;
        line-height: 1.4;
        margin: 7px 0 0;
      }
    `,
  ],
})
export class ReteNode implements OnChanges {
  protected readonly fitNodeSelect = fitNodeSelectOverlay;
  @Input() data!: StrategyNode;
  @Input() emit!: (event: any) => void;
  @Input() rendered!: () => void;
  seed = 0;
  readonly inputOrder = (left: { key: string }, right: { key: string }) => {
    const order: Record<string, number> = {
      funds: 0,
      of: 0,
      left: 0,
      conditions: 0,
      cash: 1,
      budget: 1,
      rankBy: 1,
      right: 1,
      risk: 2,
      condition: 2,
    };
    return (order[left.key] ?? 0) - (order[right.key] ?? 0) || left.key.localeCompare(right.key);
  };
  readonly outputOrder = (left: { key: string }, right: { key: string }) =>
    Number(right.key === 'output') - Number(left.key === 'output') ||
    left.key.localeCompare(right.key);
  changeCash(cashId: string | null) {
    this.data.changeSetup({ ...this.data.setup, cashId });
  }
  get cashOptions() {
    return this.data.instruments.filter((item) => !item.isVolatilityIndex);
  }
  get portfolioScopeLabel() {
    return this.data.dto.kind === 'Portfolio'
      ? 'Фінальний розподіл'
      : this.data.dto.kind === 'Cash'
        ? 'Залишок бюджету'
        : this.data.dto.kind === 'FundSelection'
          ? 'Спільний відбір'
          : 'Спільний розподіл';
  }
  outputLabel(key: string) {
    if (this.data.dto.kind === 'Cash') return key === 'series' ? 'Ряд кешу' : 'У кеші';
    if (this.data.dto.kind === 'RiskResult') return 'Контроль';
    if (this.data.dto.kind === 'SelectionResult')
      return key === 'cash' ? 'Залишок' : 'Фонди й ваги';
    if (this.data.dto.kind === 'MarketResult') return 'Бюджет';
    if (this.data.dto.kind === 'FundSelection') return 'Відібрані';
    if (['EntryResult', 'ExitResult'].includes(this.data.dto.kind)) return 'Кандидати';
    return this.data.outputs[key]?.socket.name === 'Number' ? 'Число' : 'Так / ні';
  }
  get scope() {
    return evaluationScope(this.data.dto, this.data.graph, this.data.setup);
  }
  get scopeTitle() {
    if (this.scope === 'per-fund')
      return this.data.dto.kind === 'RiskResult'
        ? 'Окрема перевірка кожної утримуваної позиції'
        : `Окреме обчислення для кожного з ${this.data.setup.funds.length} вибраних фондів`;
    if (this.data.dto.kind === 'Portfolio')
      return 'Поєднує ваги фондів, кеш і налаштування контролю ризику';
    if (this.data.dto.kind === 'Cash')
      return 'Отримує невикористаний бюджет після розподілу ваг фондів';
    return this.scope === 'portfolio'
      ? 'Збирає оцінки фондів, порівнює їх між собою та розподіляє ваги'
      : 'Один результат, спільний для всіх вибраних фондів';
  }
  get style() {
    return nodeStyle(this.data.dto);
  }
  get title() {
    if (this.data.dto.kind === 'Indicator')
      return (
        this.data.vocabulary.indicators.find(
          (i) => i.code === this.data.dto.parameters['indicator'],
        )?.label ?? 'Індикатор'
      );
    if (this.data.dto.kind.endsWith('Result'))
      return blocks.find((b) => b.kind === this.data.dto.block)?.result ?? 'Результат';
    return (
      this.data.vocabulary.conditions.find((c) => c.code === this.data.dto.kind.toLowerCase())
        ?.label ?? this.style.label
    );
  }
  private get resultBlocks() {
    return [...new Set(resultsUsing(this.data.graph, this.data.id).map((node) => node.block))];
  }
  get shortRole() {
    const roles = this.resultBlocks;
    if (roles.length > 1) return 'Спільний';
    return (
      (
        {
          MarketFilter: 'Ринок',
          Entry: 'Вхід',
          Exit: 'Вихід',
          Selection: 'Ваги',
          Risk: 'Ризик',
        } as Record<string, string>
      )[roles[0] ?? ''] ?? ''
    );
  }
  get role() {
    if (this.data.dto.kind === 'Portfolio') return 'Підсумок стратегії';
    if (this.data.dto.kind === 'Cash') return 'Кеш портфеля';
    return (
      this.resultBlocks
        .map((kind) => blocks.find((block) => block.kind === kind)?.title)
        .filter(Boolean)
        .join(' · ') || 'Ще не підключено'
    );
  }
  inputLabel(key: string) {
    if (key === 'condition' && this.data.dto.kind === 'EntryResult') return 'Умова купівлі';
    if (key === 'condition' && this.data.dto.kind === 'ExitResult') return 'Умова продажу';
    if (key === 'rankBy') return 'Оцінка фонду';
    if (key === 'funds') return this.data.dto.kind === 'Portfolio' ? 'Фонди й ваги' : 'Кандидати';
    if (key === 'cash') return 'Кеш';
    if (key === 'risk') return 'Ризик';
    if (key === 'budget') return 'Бюджет';
    if (key === 'remainder') return 'Залишок';
    return (
      (
        {
          of: 'Ряд',
          condition: 'Умова',
          left: 'Значення',
          right: 'Порівняти з',
          value: 'Значення',
          conditions: 'Умови',
          funds: 'Фонди',
        } as Record<string, string>
      )[key] ?? key
    );
  }
  ifKeyboardInput(event: MouseEvent, key: string) {
    if (event.detail === 0) this.data.inputClick(key);
  }
  ifKeyboardOutput(event: MouseEvent, key: string) {
    if (event.detail === 0) this.data.outputClick(key);
  }
  ngOnChanges() {
    this.seed++;
    requestAnimationFrame(() => this.rendered?.());
  }
}
