import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';

const labels: Record<string, string> = { All: 'Усі умови', Any: 'Будь-яка умова', Not: 'НЕ' };

@Component({
  selector: 'app-logic-node',
  template: `<div class="font-semibold">{{ labels[node().kind] }}</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogicNode {
  readonly node = input.required<NodeDto>();
  protected readonly labels = labels;
}
