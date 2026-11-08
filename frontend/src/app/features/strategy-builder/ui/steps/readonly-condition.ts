import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Button } from 'primeng/button';

@Component({
  selector: 'app-readonly-condition',
  imports: [TranslocoPipe, Button],
  template: `<p class="mb-4 text-sm text-muted-color">{{ sentence() }}</p>
    @if (editable()) {
      <p-button
        [label]="'builder.steps.editInDiagram' | transloco"
        icon="pi pi-sitemap"
        (onClick)="edit.emit()"
      />
    } @else {
      <p class="text-sm text-muted-color">{{ 'builder.steps.diagramOnly' | transloco }}</p>
    }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReadonlyCondition {
  readonly sentence = input.required<string>();
  readonly editable = input(true);
  readonly edit = output<void>();
}
