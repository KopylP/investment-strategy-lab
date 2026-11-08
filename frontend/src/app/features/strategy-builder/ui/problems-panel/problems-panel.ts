import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DraftProblemDto } from '../../models/draft-dto';

@Component({
  selector: 'app-builder-problems-panel',
  imports: [TranslocoPipe],
  template: `<section
    class="card mb-0"
    [attr.aria-label]="'builder.problems.title' | transloco"
    [class.compact]="compact()"
    aria-live="polite"
  >
    <h2 class="mb-3 text-lg font-semibold">{{ 'builder.problems.title' | transloco }}</h2>
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
      <p class="text-sm text-muted-color">{{ 'builder.problems.none' | transloco }}</p>
    }
  </section>`,
  styles: `
    .compact {
      padding: 0;
      margin: 0;
      background: transparent;
    }
    .compact h2 {
      font-size: 14px;
      line-height: 1.4;
      margin: 0 0 8px;
    }
    .compact button,
    .compact p {
      font-size: 12px;
      line-height: 1.5;
    }
    .compact button {
      margin-bottom: 4px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProblemsPanel {
  readonly compact = input(false);
  readonly problems = input.required<DraftProblemDto[]>();
  readonly choose = output<DraftProblemDto>();
}
