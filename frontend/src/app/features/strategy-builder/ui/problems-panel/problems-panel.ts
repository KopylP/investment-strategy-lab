import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DraftProblemDto } from '../../models/draft-dto';

@Component({
  selector: 'app-builder-problems-panel',
  template: `<section class="card mb-0" aria-label="Що перевірити" aria-live="polite">
    <h2 class="mb-3 text-lg font-semibold">Що перевірити</h2>
    @for (problem of problems(); track $index) {
      <button
        type="button"
        class="mb-2 flex w-full items-start gap-2 rounded p-2 text-left text-sm hover:bg-surface-100 dark:hover:bg-surface-800"
        (click)="choose.emit(problem)"
      >
        <i
          class="pi mt-1"
          [class.pi-times-circle]="problem.severity === 'Error'"
          [class.pi-exclamation-triangle]="problem.severity === 'Warning'"
          [class.text-red-500]="problem.severity === 'Error'"
          [class.text-amber-500]="problem.severity === 'Warning'"
          aria-hidden="true"
        ></i>
        <span>{{ problem.message }}</span>
      </button>
    } @empty {
      <p class="text-sm text-muted-color">Проблем не виявлено.</p>
    }
  </section>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProblemsPanel {
  readonly problems = input.required<DraftProblemDto[]>();
  readonly choose = output<DraftProblemDto>();
}
