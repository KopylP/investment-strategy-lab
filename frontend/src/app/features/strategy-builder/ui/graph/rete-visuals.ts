import { Component, Input, OnChanges } from '@angular/core';
import { ClassicPreset } from 'rete';
import { StrategyConnection } from './rete-model';
import { wireStyles } from './graph-visuals';
import { SocketType } from '../../models/vocabulary-dto';

@Component({
  selector: 'app-rete-socket',
  template: `<span
    [style.background]="color"
    [class.number]="data.name === 'Number'"
    [class.funds]="data.name === 'Funds'"
  ></span>`,
  styles: [
    `
      :host {
        display: block;
        width: 20px;
        height: 20px;
      }
      span {
        display: block;
        width: 20px;
        height: 20px;
        border: 3px solid var(--p-content-background, white);
        border-radius: 50%;
        box-shadow: 0 0 0 1px currentColor;
      }
      .number {
        border-radius: 4px;
      }
      .funds {
        border-radius: 3px;
        transform: rotate(45deg);
      }
    `,
  ],
})
export class ReteSocket implements OnChanges {
  @Input() rendered!: () => void;
  ngOnChanges() {
    requestAnimationFrame(() => this.rendered?.());
  }
  @Input() data!: ClassicPreset.Socket;
  get color() {
    return wireStyles[this.data.name as SocketType]?.color ?? '#7c3aed';
  }
}
@Component({
  selector: 'app-rete-connection',
  template: `<svg>
    <path class="hit" [attr.d]="path" (dblclick)="remove()">
      <title>Двічі натисніть, щоб видалити зв’язок</title>
    </path>
    <path
      [attr.d]="path"
      [attr.stroke]="style.color"
      [attr.stroke-width]="style.width"
      [attr.stroke-dasharray]="style.dash"
      [attr.data-edge-from]="data.source"
      [attr.data-edge-to]="data.target"
      [attr.data-edge-input]="data.targetInput"
    />
  </svg>`,
  styles: [
    `
      :host {
        position: absolute;
        top: 0;
        left: 0;
      }
      svg {
        position: absolute;
        overflow: visible;
        pointer-events: none;
        width: 1px;
        height: 1px;
      }
      path {
        fill: none;
      }
      .hit {
        stroke: transparent;
        stroke-width: 18px;
        pointer-events: stroke;
        cursor: pointer;
      }
    `,
  ],
})
export class ReteConnection {
  @Input() data!: StrategyConnection & { type?: SocketType; remove?: () => void };
  @Input() start: unknown;
  @Input() end: unknown;
  @Input() path = '';
  get style() {
    return wireStyles[this.data.type ?? 'Boolean'];
  }
  remove() {
    this.data.remove?.();
  }
}
