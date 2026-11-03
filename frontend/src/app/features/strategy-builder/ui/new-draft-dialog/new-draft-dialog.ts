import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TemplateDto } from '../../models/vocabulary-dto';

@Component({
  selector: 'app-new-builder-draft-dialog',
  imports: [FormsModule, Button, Dialog, InputText, Select],
  template: `
    <p-dialog
      header="Нова стратегія"
      [modal]="true"
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [style]="{ width: '34rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      (onShow)="reset()"
    >
      <div class="flex flex-col gap-4">
        <div class="flex flex-col gap-2">
          <label for="builder-draft-name" class="font-medium">Назва</label>
          <input
            pInputText
            id="builder-draft-name"
            [ngModel]="name()"
            (ngModelChange)="name.set($event)"
            placeholder="Наприклад, Моментум із фільтром тренду"
            autofocus
          />
        </div>
        <div class="flex flex-col gap-2">
          <label for="builder-template" class="font-medium">Почати</label>
          <p-select
            inputId="builder-template"
            [options]="options()"
            optionLabel="label"
            optionValue="value"
            [ngModel]="template()"
            (ngModelChange)="template.set($event)"
            fluid
          />
          @if (selectedDescription()) {
            <small class="text-muted-color">{{ selectedDescription() }}</small>
          }
        </div>
      </div>
      <ng-template #footer>
        <p-button
          label="Скасувати"
          [text]="true"
          severity="secondary"
          (onClick)="visible.set(false)"
        />
        <p-button
          label="Створити"
          icon="pi pi-plus"
          [loading]="busy()"
          [disabled]="!name().trim()"
          (onClick)="create.emit({ name: name().trim(), template: template() })"
        />
      </ng-template>
    </p-dialog>
  `,
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewDraftDialog {
  readonly visible = model(false);
  readonly templates = input.required<TemplateDto[]>();
  readonly busy = input(false);
  readonly create = output<{ name: string; template: string | null }>();

  protected readonly name = signal('');
  protected readonly template = signal<string | null>(null);
  protected readonly options = computed(() => [
    { label: 'З порожньої схеми', value: null },
    ...this.templates().map((template) => ({ label: template.name, value: template.code })),
  ]);
  protected readonly selectedDescription = computed(
    () => this.templates().find((template) => template.code === this.template())?.description ?? '',
  );

  protected reset(): void {
    this.name.set('');
    this.template.set(null);
  }
}
