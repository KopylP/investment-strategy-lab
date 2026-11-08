import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { GraphDto, NodeDto } from '../../models/draft-dto';

@Component({
  selector: 'app-gate-outcome',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="gate-outcome" [attr.data-gate]="entry() ? 'entry' : 'exit'">
      @if (connected()) {
        <div class="outcome">
          <span class="answer yes"
            ><i class="pi pi-check"></i> {{ 'builder.inspector.gate.yes' | transloco }}</span
          ><i class="pi pi-arrow-right arrow"></i
          ><span>{{
            entry()
              ? ('builder.inspector.gate.toCandidates' | transloco)
              : ('builder.inspector.gate.sellFund' | transloco)
          }}</span>
        </div>
        <div class="outcome">
          <span class="answer no"
            ><i class="pi pi-minus"></i> {{ 'builder.inspector.gate.no' | transloco }}</span
          ><i class="pi pi-arrow-right arrow"></i
          ><span>{{
            entry()
              ? ('builder.inspector.gate.dontBuy' | transloco)
              : ('builder.inspector.gate.toShared' | transloco)
          }}</span>
        </div>
      } @else {
        <div class="empty">
          <i class="pi pi-link"></i
          ><span>{{
            entry()
              ? ('builder.inspector.gate.noCondition' | transloco)
              : ('builder.inspector.gate.noSellCondition' | transloco)
          }}</span>
        </div>
      }
      <div class="next-step">
        <i class="pi pi-th-large"></i
        ><span>{{
          entry()
            ? ('builder.inspector.gate.next' | transloco)
            : ('builder.inspector.gate.sharedMayExclude' | transloco)
        }}</span>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .gate-outcome {
        display: flex;
        flex-direction: column;
        gap: 7px;
        font-size: 11px;
      }
      .outcome {
        display: flex;
        align-items: center;
        gap: 6px;
        line-height: 1.4;
      }
      .answer {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 4px;
        border: 1px solid var(--p-content-border-color);
        border-radius: 5px;
        min-width: 45px;
        padding: 3px 5px;
        flex-shrink: 0;
      }
      .answer i,
      .arrow {
        font-size: 10px;
      }
      .yes {
        color: var(--p-text-color);
        background: var(--p-content-hover-background);
      }
      .no,
      .arrow {
        color: var(--p-text-muted-color);
      }
      .next-step,
      .empty {
        display: flex;
        align-items: flex-start;
        gap: 5px;
        color: var(--p-text-muted-color);
        line-height: 1.4;
      }
      .next-step {
        margin-top: 2px;
        padding-top: 7px;
        border-top: 1px solid var(--p-content-border-color);
        font-size: 10px;
      }
      .next-step i,
      .empty i {
        margin-top: 2px;
        font-size: 10px;
      }
    `,
  ],
})
export class GateOutcome {
  readonly node = input.required<NodeDto>();
  readonly graph = input.required<GraphDto>();
  protected readonly entry = computed(() => this.node().kind === 'EntryResult');
  protected readonly connected = computed(() =>
    this.graph().links.some((link) => link.to === this.node().id && link.input === 'condition'),
  );
}
