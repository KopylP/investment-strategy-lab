import { translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';

const labels: Record<string, string> = {
  All: translate('builder.graph.node.all'),
  Any: translate('builder.graph.node.any'),
  Not: translate('builder.graph.node.not'),
};

@Component({
  selector: 'app-logic-node',
  template: `<div class="font-semibold">{{ labels[node().kind] }}</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogicNode {
  readonly node = input.required<NodeDto>();
  protected readonly labels = labels;
}
