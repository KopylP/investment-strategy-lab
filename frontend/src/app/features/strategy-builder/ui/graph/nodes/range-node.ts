import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';
import { parameterSummary } from '../node-view';

@Component({
  selector: 'app-range-node',
  imports: [TranslocoPipe],
  template: `<div class="font-semibold">
      {{
        node().kind === 'Between'
          ? ('builder.graph.node.between' | transloco)
          : ('builder.graph.node.crossed' | transloco)
      }}
    </div>
    <div class="mt-2 text-xs">{{ summary() }}</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RangeNode {
  readonly node = input.required<NodeDto>();
  protected summary = () => parameterSummary(this.node());
}
