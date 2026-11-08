import { TranslocoPipe } from '@jsverse/transloco';
import { Component, Input, OnChanges } from '@angular/core';
import { ClassicPreset } from 'rete';
import { StrategyConnection } from './rete-model';
import { wireStyles } from './graph-visuals';
import { EdgeHandle } from './edge-routing';
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
  imports: [TranslocoPipe],
  template: `<svg [class.selected]="data.selected" [style.opacity]="data.branchDimmed ? 0.2 : 1">
    <path
      class="hit"
      [attr.d]="path"
      tabindex="0"
      role="button"
      [attr.aria-label]="'builder.graph.chooseLink' | transloco"
      [attr.aria-pressed]="!!data.selected"
      [attr.data-edge-hit]="data.id"
      (pointerdown)="$event.stopPropagation()"
      (click)="choose($event)"
      (keydown.enter)="choose($event)"
      (keydown.space)="choose($event)"
      (dblclick)="remove()"
    >
      <title>{{ 'builder.graph.deleteLinkHint' | transloco }}</title>
    </path>
    @if (data.selected) {
      <path class="selection-glow" [attr.d]="path" />
    }
    <path
      [attr.d]="path"
      [attr.stroke]="style.color"
      [attr.stroke-width]="data.selected ? style.width + 2 : style.width"
      [attr.stroke-dasharray]="style.dash"
      stroke-linecap="round"
      stroke-linejoin="round"
      [attr.marker-end]="'url(#' + markerId + ')'"
      [attr.data-edge-selected]="data.selected || null"
      [attr.data-edge-from]="data.source"
      [attr.data-edge-to]="data.target"
      [attr.data-edge-input]="data.targetInput"
      [attr.data-edge-output]="data.sourceOutput"
    />
    @if (data.selected) {
      @for (handle of data.handles ?? []; track handle.segment) {
        <circle
          class="route-handle"
          [attr.cx]="handle.x"
          [attr.cy]="handle.y"
          [attr.r]="data.handleRadius ?? 7"
          [style.stroke-width]="(data.handleRadius ?? 6) / 3"
          tabindex="0"
          role="slider"
          [attr.aria-label]="
            (handle.axis === 'x'
              ? 'builder.graph.moveSegmentHorizontal'
              : 'builder.graph.moveSegmentVertical'
            ) | transloco
          "
          [attr.aria-valuenow]="handle[handle.axis]"
          [attr.aria-orientation]="handle.axis === 'x' ? 'horizontal' : 'vertical'"
          (keydown)="adjustHandle($event, handle)"
          [attr.data-route-segment]="handle.segment"
          [attr.data-route-axis]="handle.axis"
          [style.cursor]="handle.axis === 'x' ? 'ew-resize' : 'ns-resize'"
          (pointerdown)="startDrag($event, handle)"
          (pointermove)="moveDrag($event)"
          (pointerup)="endDrag($event, false)"
          (pointercancel)="endDrag($event, true)"
          (click)="$event.stopPropagation()"
        />
      }
    }
    <defs>
      <marker
        [attr.id]="markerId"
        viewBox="0 0 10 10"
        refX="9"
        refY="5"
        markerWidth="7"
        markerHeight="7"
        markerUnits="userSpaceOnUse"
        orient="auto-start-reverse"
      >
        <path d="M 1 1 L 9 5 L 1 9" [attr.stroke]="style.color" stroke-width="1.5" />
      </marker>
    </defs>
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
      .selection-glow {
        stroke: var(--p-primary-color, #818cf8);
        stroke-width: 16px;
        opacity: 0.45;
      }
      .route-handle {
        pointer-events: all;
        fill: var(--p-content-background, white);
        stroke: var(--p-primary-color, #818cf8);
        stroke-width: 2px;
      }
      .route-handle:focus-visible {
        outline: none;
        stroke-width: 4px;
      }
      .hit:focus-visible {
        outline: none;
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
  @Input() data!: StrategyConnection & {
    type?: SocketType;
    remove?: () => void;
  };
  @Input() path = '';
  private dragging = false;
  choose(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.data.choose?.();
  }
  startDrag(event: PointerEvent, handle: EdgeHandle) {
    if (event.button !== 0) return;
    event.preventDefault();
    event.stopPropagation();
    this.dragging = true;
    (event.currentTarget as Element).setPointerCapture(event.pointerId);
    this.data.dragStart?.(handle.segment);
  }
  moveDrag(event: PointerEvent) {
    if (!this.dragging) return;
    event.stopPropagation();
    this.data.dragMove?.({ x: event.clientX, y: event.clientY });
  }
  endDrag(event: PointerEvent, cancel: boolean) {
    if (!this.dragging) return;
    this.dragging = false;
    event.stopPropagation();
    this.data.dragEnd?.(cancel);
  }
  adjustHandle(event: KeyboardEvent, handle: EdgeHandle) {
    const direction =
      handle.axis === 'x'
        ? event.key === 'ArrowLeft'
          ? -1
          : event.key === 'ArrowRight'
            ? 1
            : 0
        : event.key === 'ArrowUp'
          ? -1
          : event.key === 'ArrowDown'
            ? 1
            : 0;
    if (!direction) return;
    event.preventDefault();
    event.stopPropagation();
    const element = event.currentTarget as SVGCircleElement;
    const matrix = element.getScreenCTM();
    if (!matrix) return;
    const point = new DOMPoint(handle.x, handle.y);
    point[handle.axis] += direction * (event.shiftKey ? 32 : 8);
    const screen = point.matrixTransform(matrix);
    this.data.dragStart?.(handle.segment);
    this.data.dragMove?.({ x: screen.x, y: screen.y });
    this.data.dragEnd?.(false);
  }
  get markerId() {
    return `edge-arrow-${this.data.id}`;
  }
  get style() {
    return wireStyles[this.data.type ?? 'Boolean'];
  }
  remove() {
    this.data.remove?.();
  }
}
