import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Button } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';
import { SetupSummaryVm } from '../../models/draft-vm';

@Component({
  selector: 'app-builder-setup-row',
  imports: [Button, Tooltip],
  template: `
    <div class="card mb-4 flex min-w-0 flex-wrap items-center gap-x-6 gap-y-3 py-3">
      <div class="min-w-0 flex-1">
        <div class="flex min-w-0 flex-wrap items-center gap-x-5 gap-y-2 text-sm">
          <span class="min-w-0" [pTooltip]="fundTooltip()">
            <span class="text-muted-color">Фонди</span>
            <strong class="ml-1">{{ summary().fundCount }}</strong>
            @if (summary().fundLabels.length) {
              <span class="ml-2 truncate">{{ summary().fundLabels.join(', ') }}</span>
              @if (summary().moreFunds) {
                <span class="text-muted-color"> +{{ summary().moreFunds }}</span>
              }
            }
          </span>
          <span class="truncate" [pTooltip]="summary().benchmark">
            <span class="text-muted-color">Бенчмарк</span>
            <span class="ml-1">{{ summary().benchmark }}</span>
          </span>
          <span class="truncate" [pTooltip]="summary().cash">
            <span class="text-muted-color">Кеш</span>
            <span class="ml-1">{{ summary().cash }}</span>
          </span>
          <button
            type="button"
            class="min-w-0 cursor-pointer truncate border-0 bg-transparent p-0 text-left text-color"
            [pTooltip]="summary().reports"
            (click)="editReports.emit()"
          >
            <span class="text-muted-color">Рапорти:</span>
            {{ summary().reports }}
          </button>
        </div>
      </div>
      <p-button
        label="Змінити"
        icon="pi pi-pencil"
        severity="secondary"
        [outlined]="true"
        size="small"
        (onClick)="editSetup.emit()"
      />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetupRow {
  readonly summary = input.required<SetupSummaryVm>();
  readonly editSetup = output<void>();
  readonly editReports = output<void>();

  protected fundTooltip(): string {
    return this.summary().fundLabels.join(', ');
  }
}
