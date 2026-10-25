import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { ProgressBar } from 'primeng/progressbar';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { DateTimePipe, MoneyPipe, PercentPipe, PeriodPipe } from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { OverviewStore } from '../../data-access/overview-store';

@Component({
  selector: 'app-overview-page',
  imports: [RouterLink, Button, ProgressBar, Skeleton, Tag, PageHeader, SignedValue, MoneyPipe, PercentPipe, PeriodPipe, DateTimePipe],
  providers: [OverviewStore],
  templateUrl: './overview-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewPage {
  protected readonly store = inject(OverviewStore);
}
