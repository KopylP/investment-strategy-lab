import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';
import { textParameter } from '../node-view';

const operators: Record<string, string> = {
  gt: '>',
  gte: '≥',
  lt: '<',
  lte: '≤',
  eq: '=',
  ne: '≠',
};

@Component({
  selector: 'app-compare-node',
  template: `<div class="font-semibold">Порівняння</div>
    <div class="mt-2 text-sm">A {{ operator() }} {{ value() }}</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompareNode {
  readonly node = input.required<NodeDto>();
  protected operator = () => operators[textParameter(this.node(), 'operator') ?? ''] ?? '?';
  protected value = () => this.node().parameters['value'] ?? 'B';
}
