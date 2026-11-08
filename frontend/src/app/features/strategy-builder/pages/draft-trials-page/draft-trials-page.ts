import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { DateTimePipe, PercentPipe } from '../../../../shared/pipes/format-pipes';
import { formatDate } from '../../../../shared/pipes/format';
import { hostWidth } from '../../../../shared/ui/host-width';
import { TrialRunDto } from '../../models/trial-run-dto';
import { TrialStatusVm, trialStatuses } from '../../models/trial-status-vm';
import { StrategyBacktest } from '../../../backtesting/ui/strategy-backtest/strategy-backtest';
import { TrialsStore } from '../../data-access/trials-store';
import { TrialComparison } from '../../ui/trial-comparison/trial-comparison';
import { ExperimentStore } from '../../data-access/experiment-store';
import { ExperimentResults } from '../../ui/experiment-results/experiment-results';

const tableLayoutMinimumWidth = 672;

@Component({
  selector: 'app-draft-trials-page',
  imports: [
    DecimalPipe,
    NgTemplateOutlet,
    Message,
    TableModule,
    PageHeader,
    DateTimePipe,
    PercentPipe,
    StrategyBacktest,
    TrialComparison,
    ExperimentResults,
  ],
  providers: [TrialsStore, ExperimentStore],
  templateUrl: './draft-trials-page.html',
  styleUrl: './draft-trials-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftTrialsPage {
  readonly draftId = input.required<string>();
  protected readonly store = inject(TrialsStore);
  private readonly experiments = inject(ExperimentStore);
  private readonly route = inject(ActivatedRoute);
  private readonly width = hostWidth();
  protected readonly listLayout = computed(() => this.width() < tableLayoutMinimumWidth);

  constructor() {
    effect(() => {
      const runs = this.store.selectedRuns();
      const id = runs.length === 1 ? runs[0].experimentId ?? null : null;
      untracked(() => this.experiments.show(id));
    });
    effect(() => {
      const id = this.draftId();
      const run = Number(this.route.snapshot.queryParamMap.get('run'));
      untracked(() => this.store.show(id, run > 0 ? run : null));
    });
  }

  protected isSelected(run: TrialRunDto): boolean {
    return this.store.selected().includes(run.number);
  }

  protected statusOf(run: TrialRunDto): TrialStatusVm {
    return trialStatuses[run.status];
  }

  protected periodOf(run: TrialRunDto): string {
    const period = run.experiment?.period;
    if (!period) return 'Автоматичний період';

    return `${formatDate(period.start)} – ${formatDate(period.end)}`;
  }
}
