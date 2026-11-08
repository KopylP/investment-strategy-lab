import { TranslocoPipe } from '@jsverse/transloco';
import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { MoneyPipe, PercentPipe, PeriodPipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { OverviewStore } from '../../data-access/overview-store';

import { OverviewValueCard } from '../../ui/overview-value-card/overview-value-card';

import { OverviewMetrics } from '../../ui/overview-metrics/overview-metrics';

import { OverviewTaxProgress } from '../../ui/overview-tax-progress/overview-tax-progress';

import { OverviewDetailsGrid } from '../../ui/overview-details-grid/overview-details-grid';

@Component({
  selector: 'app-overview-page',
  imports: [TranslocoPipe, 
    TaxMoneyPipe,
    OverviewDetailsGrid,
    OverviewTaxProgress,
    OverviewMetrics,
    OverviewValueCard,
    RouterLink,
    Button,
    Message,
    Skeleton,
    PageHeader,
    MoneyPipe,
    PercentPipe,
    PeriodPipe,
  ],
  providers: [OverviewStore],
  templateUrl: './overview-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewPage {
  protected readonly store = inject(OverviewStore);
}
