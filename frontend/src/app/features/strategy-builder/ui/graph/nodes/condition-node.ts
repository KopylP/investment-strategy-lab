import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';
import { VocabularyDto } from '../../../models/vocabulary-dto';
import { conditionLabel, parameterSummary } from '../node-view';

@Component({
  selector: 'app-condition-node',
  template: `<div class="font-semibold">{{ label() }}</div>
    @if (summary()) {
      <div class="mt-2 text-xs">{{ summary() }}</div>
    }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConditionNode {
  readonly node = input.required<NodeDto>();
  readonly vocabulary = input.required<VocabularyDto>();
  protected label = () => conditionLabel(this.node(), this.vocabulary());
  protected summary = () => parameterSummary(this.node());
}
