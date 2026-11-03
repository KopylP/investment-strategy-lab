import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';

@Component({
  selector: 'app-signal-node',
  template: `<div class="font-semibold">Сигнал</div>
    <div class="mt-2 text-xs">
      {{ node().parameters['state'] === false ? 'не активний' : 'активний' }}
    </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignalNode {
  readonly node = input.required<NodeDto>();
}
