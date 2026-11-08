import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';

@Component({
  selector: 'app-copy-entry-dialog',
  imports: [TranslocoPipe, FormsModule, Button, Dialog, InputNumber],
  template: `
    <p-dialog
      [header]="'builder.graph.copyEntry.title' | transloco"
      [modal]="true"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [style]="{ width: '28rem' }"
      [breakpoints]="{ '520px': '95vw' }"
      (onShow)="margin.set(2)"
    >
      <p class="mb-4 text-sm text-muted-color">
        {{ 'builder.graph.copyEntry.hint' | transloco }}
      </p>
      <p class="mb-4 text-sm">{{ 'builder.graph.copyEntry.replaceHint' | transloco }}</p>
      <div class="flex flex-col gap-2">
        <label for="copy-entry-margin" class="font-medium">{{ 'builder.graph.copyEntry.margin' | transloco }}</label>
        <p-inputnumber
          inputId="copy-entry-margin"
          [ngModel]="margin()"
          (ngModelChange)="margin.set($event ?? 0)"
          suffix=" %"
          [min]="0"
          [max]="50"
          [maxFractionDigits]="2"
          fluid
        />
      </div>
      <ng-template #footer>
        <p-button
          [label]="'common.cancel' | transloco"
          [text]="true"
          severity="secondary"
          (onClick)="visible.set(false)"
        />
        <p-button [label]="'builder.graph.copyEntry.replace' | transloco" icon="pi pi-copy" (onClick)="copy.emit(margin() / 100)" />
      </ng-template>
    </p-dialog>
  `,
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CopyEntryDialog {
  readonly visible = model(false);
  readonly copy = output<number>();
  protected readonly margin = signal(2);
}
