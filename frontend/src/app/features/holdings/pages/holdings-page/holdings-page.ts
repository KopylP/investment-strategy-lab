import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';

import { Tooltip } from 'primeng/tooltip';
import { DatePipe, PercentPipe, QuantityPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { Money } from '../../../../shared/ui/money/money';
import { PositionChart } from '../../ui/position-chart';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { HoldingsStore } from '../../data-access/holdings-store';

@Component({
  selector: 'app-holdings-page',
  imports: [
    FormsModule, RouterLink, Button, DatePicker, Message, Skeleton, TableModule, Tooltip, PageHeader, EmptyState, SignedValue, Money, PositionChart,
    PercentPipe, QuantityPipe, SignedPercentPipe, DatePipe,
  ],
  providers: [HoldingsStore],
  templateUrl: './holdings-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoldingsPage {
  protected readonly store = inject(HoldingsStore);
  protected readonly today = new Date();
}
