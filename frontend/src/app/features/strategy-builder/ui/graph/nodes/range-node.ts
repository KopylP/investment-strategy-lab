import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';
import { parameterSummary } from '../node-view';

@Component({
  selector: 'app-range-node',
  template: `<div class="font-semibold">
      {{ node().kind === 'Between' ? 'Між межами' : 'Перетин рівня' }}
    </div>
    <div class="mt-2 text-xs">{{ summary() }}</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RangeNode {
  readonly node = input.required<NodeDto>();
  protected summary = () => parameterSummary(this.node());
}
