import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';
import { VocabularyDto } from '../../../models/vocabulary-dto';
import { indicatorLabel, parameterSummary, seriesLabel } from '../node-view';

@Component({
  selector: 'app-indicator-node',
  imports: [TranslocoPipe],
  template: `<div class="font-semibold">{{ label() }}</div>
    <div class="mt-1 text-xs text-muted-color">
      {{
        'builder.graph.node.forWhat'
          | transloco: { series: series() || ('builder.graph.node.notChosen' | transloco) }
      }}
    </div>
    @if (parameters()) {
      <div class="mt-2 text-xs">{{ parameters() }}</div>
    }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IndicatorNode {
  readonly node = input.required<NodeDto>();
  readonly vocabulary = input.required<VocabularyDto>();
  protected label = () => indicatorLabel(this.node(), this.vocabulary());
  protected series = () => seriesLabel(this.node());
  protected parameters = () => parameterSummary(this.node());
}
