import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Button } from 'primeng/button';

@Component({
  selector: 'app-readonly-condition',
  imports: [Button],
  template: `<p class="mb-4 text-sm text-muted-color">{{ sentence() }}</p>
    <p-button label="Редагувати в схемі" icon="pi pi-sitemap" (onClick)="edit.emit()" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReadonlyCondition {
  readonly sentence = input.required<string>();
  readonly edit = output<void>();
}
