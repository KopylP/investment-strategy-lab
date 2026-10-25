import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { StrategiesStore } from '../../data-access/strategies-store';

@Component({
  selector: 'app-strategy-list-page',
  imports: [RouterLink, Button, Message, Skeleton, Tag, PageHeader, EmptyState, DatePipe],
  providers: [StrategiesStore],
  templateUrl: './strategy-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StrategyListPage {
  protected readonly store = inject(StrategiesStore);
}
