import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NodeDto } from '../../../models/draft-dto';

@Component({
  selector: 'app-signal-node',
  imports: [TranslocoPipe],
  template: `<div class="font-semibold">{{ 'builder.graph.node.mySignal' | transloco }}</div>
    <div class="mt-2 text-xs">
      {{
        node().parameters['state'] === 'inactive'
          ? ('builder.graph.node.signalInactive' | transloco)
          : ('builder.graph.node.signalActive' | transloco)
      }}
    </div>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignalNode {
  readonly node = input.required<NodeDto>();
}
