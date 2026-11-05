import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { wireStyles } from '../graph-visuals';
import { SocketType } from '../../../models/vocabulary-dto';

@Component({
  selector: 'app-socket-shape',
  template: `<span
    class="block h-3.5 w-3.5 border-2 border-current bg-surface-0 dark:bg-surface-900"
    [style.color]="styles[type()].color"
    [class.rounded-full]="type() === 'Number'"
    [class.rotate-45]="type() === 'Boolean'"
    [class.rounded-sm]="type() === 'Funds'"
    [attr.title]="label() + ' · ' + styles[type()].label"
  ></span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SocketShape {
  protected readonly styles = wireStyles;
  readonly type = input.required<SocketType>();
  readonly label = input.required<string>();
}
