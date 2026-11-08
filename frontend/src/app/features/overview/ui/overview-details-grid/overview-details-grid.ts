import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { DrawdownChart } from '../../ui/drawdown-chart/drawdown-chart';
import { AllocationCard } from '../../ui/allocation-card/allocation-card';
import { Money } from '../../../../shared/ui/money/money';
import {
  PercentPipe,
  SignedPercentPipe,
  DateTimePipe,
} from '../../../../shared/pipes/format-pipes';
import { DashboardDto } from '../../data-access/dashboard-api';
import { OverviewStore } from '../../data-access/overview-store';

@Component({
  selector: 'app-overview-details-grid',
  imports: [
    TranslocoPipe,
    RouterLink,
    Button,
    Skeleton,
    TableModule,
    Tag,
    Tooltip,
    TapTooltip,
    SignedValue,
    DrawdownChart,
    AllocationCard,
    Money,
    PercentPipe,
    SignedPercentPipe,
    DateTimePipe,
  ],
  templateUrl: './overview-details-grid.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewDetailsGrid {
  readonly dashboard = input.required<DashboardDto>();
  protected readonly store = inject(OverviewStore);
}
