import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { TaxProgressChart } from '../../ui/tax-progress-chart/tax-progress-chart';
import { MoneyPipe, PercentPipe } from '../../../../shared/pipes/format-pipes';
import { OverviewStore } from '../../data-access/overview-store';

@Component({
  selector: 'app-overview-tax-progress',
  imports: [Message, Skeleton, SignedValue, TaxProgressChart, MoneyPipe, PercentPipe],
  templateUrl: './overview-tax-progress.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewTaxProgress {
  protected readonly store = inject(OverviewStore);
}
