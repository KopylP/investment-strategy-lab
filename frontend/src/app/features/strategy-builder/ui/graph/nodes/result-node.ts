import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';
import { parameterSummary } from '../node-view';

@Component({
  selector: 'app-result-node',
  imports: [TranslocoPipe],
  template: `<div class="text-xs font-semibold uppercase tracking-wide text-muted-color">
      {{ 'builder.graph.visual.result' | transloco }}
    </div>
    <div class="mt-1 font-semibold">{{ title() }}</div>
    @if (summary()) {
      <div class="mt-2 text-xs">{{ summary() }}</div>
    }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultNode {
  readonly node = input.required<NodeDto>();
  readonly title = input.required<string>();
  protected summary = () => parameterSummary(this.node());
}
