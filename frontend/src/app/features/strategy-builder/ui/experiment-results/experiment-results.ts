import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Message } from 'primeng/message';
import { DatePipe, MoneyPipe, PercentPipe } from '../../../../shared/pipes/format-pipes';
import { StrategyBacktest } from '../../../backtesting/ui/strategy-backtest/strategy-backtest';
import { ExperimentStore } from '../../data-access/experiment-store';
import { ExperimentJournal } from '../experiment-journal/experiment-journal';
import { RobustnessResults } from '../robustness-results/robustness-results';

@Component({
  selector: 'app-experiment-results',
  host: { class: 'contents' },
  imports: [
    TranslocoPipe,
    DecimalPipe,
    Message,
    DatePipe,
    MoneyPipe,
    PercentPipe,
    StrategyBacktest,
    ExperimentJournal,
    RobustnessResults,
  ],
  templateUrl: './experiment-results.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExperimentResults {
  protected readonly store = inject(ExperimentStore);
  protected readonly currency = computed(
    () => this.store.experiment()?.result?.baseline.currency ?? '',
  );
}
