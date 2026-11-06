import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { DateTimePipe, PercentPipe } from '../../../../shared/pipes/format-pipes';
import { StrategyBacktest } from '../../../backtesting/ui/strategy-backtest/strategy-backtest';
import { TrialsStore } from '../../data-access/trials-store';
import { TrialComparison } from '../../ui/trial-comparison/trial-comparison';

@Component({
  selector: 'app-draft-trials-page',
  imports: [
    DecimalPipe,
    Message,
    TableModule,
    PageHeader,
    DateTimePipe,
    PercentPipe,
    StrategyBacktest,
    TrialComparison,
  ],
  providers: [TrialsStore],
  templateUrl: './draft-trials-page.html',
  styleUrl: './draft-trials-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DraftTrialsPage {
  readonly draftId = input.required<string>();
  protected readonly store = inject(TrialsStore);
  private readonly route = inject(ActivatedRoute);
  protected readonly statuses: Record<string, string> = {
    Waiting: 'Очікує',
    Running: 'Обчислюється',
    Succeeded: 'Готово',
    Failed: 'Помилка',
  };

  constructor() {
    effect(() => {
      const id = this.draftId();
      const run = Number(this.route.snapshot.queryParamMap.get('run'));
      untracked(() => this.store.show(id, run > 0 ? run : null));
    });
  }
}
