import { Component, Input, OnChanges } from '@angular/core';
import { KeyValuePipe } from '@angular/common';
import { RefDirective } from 'rete-angular-plugin/21';
import { Inspector } from '../inspector/inspector';
import { StrategyNode } from './rete-model';
import { nodeStyle } from './graph-visuals';
import { blocks } from '../../models/draft-vm';
import { resultsUsing } from '../../data-access/shared-graph';

@Component({
  selector: 'app-strategy-rete-node',
  imports: [RefDirective, KeyValuePipe, Inspector],
  host: { '[style.width.px]': 'data.width' },
  template: `
    <article
      tabindex="0"
      role="group"
      [attr.aria-label]="style.label + ' · ' + role"
      [attr.data-node-id]="data.id"
      [attr.data-node-category]="style.label"
      [class.selected]="data.selected"
      [style.--node-color]="style.color"
      [class.logic]="['All', 'Any', 'Not'].includes(data.dto.kind)"
      [class.result]="data.dto.kind.endsWith('Result')"
      (click)="data.choose()"
    >
      <header [title]="role">
        <i [class]="'pi ' + style.icon"></i><strong>{{ title }}</strong>
        <span class="role">{{ shortRole }}</span>
        @if (!data.dto.kind.endsWith('Result')) {
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
      @if (data.problem; as problem) {
        <p role="alert" class="problem">{{ problem.message }}</p>
      }
      <div class="ports">
        <div>
          @for (input of data.inputs | keyvalue; track input.key) {
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
        @for (output of data.outputs | keyvalue; track output.key) {
          <div class="port-row">
            <span>{{
              output.value?.socket?.name === 'Number'
                ? 'Число'
                : output.value?.socket?.name === 'Funds'
                  ? 'Фонди'
                  : 'Так / ні'
            }}</span>
            <button
              type="button"
              class="port"
              aria-label="Почати звʼязок"
              [attr.data-socket-key]="data.id + ':output'"
              (click)="ifKeyboardOutput($event); $event.stopPropagation()"
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
      <div
        class="parameters"
        (pointerdown)="$event.stopPropagation()"
        (dblclick)="$event.stopPropagation()"
        (wheel)="$event.stopPropagation()"
      >
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
      .port {
        display: grid;
        place-items: center;
        width: 28px;
        height: 28px;
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
    `,
  ],
})
export class ReteNode implements OnChanges {
  @Input() data!: StrategyNode;
  @Input() emit!: (event: any) => void;
  @Input() rendered!: () => void;
  seed = 0;
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
    return (
      this.resultBlocks
        .map((kind) => blocks.find((block) => block.kind === kind)?.title)
        .filter(Boolean)
        .join(' · ') || 'Ще не підключено'
    );
  }
  inputLabel(key: string) {
    return (
      (
        {
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
  ifKeyboardOutput(event: MouseEvent) {
    if (event.detail === 0) this.data.outputClick();
  }
  ngOnChanges() {
    this.seed++;
    requestAnimationFrame(() => this.rendered?.());
  }
}
