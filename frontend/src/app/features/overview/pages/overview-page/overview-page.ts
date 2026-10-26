import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import { DateTimePipe, DatePipe, MoneyPipe, PeriodPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { Money } from '../../../../shared/ui/money/money';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { Viewport } from '../../../../shared/ui/viewport';
import { OverviewStore } from '../../data-access/overview-store';
import { AllocationCard } from '../../ui/allocation-card/allocation-card';
import { DrawdownChart } from '../../ui/drawdown-chart/drawdown-chart';
import { ValueChart } from '../../ui/value-chart/value-chart';

@Component({
  selector: 'app-overview-page',
  imports: [
    FormsModule, RouterLink, Button, Message, SelectButton, Skeleton, Tag, Tooltip, PageHeader, SignedValue, ValueChart, DrawdownChart, AllocationCard, Money,
    MoneyPipe, SignedPercentPipe, PeriodPipe, DateTimePipe, DatePipe,
  ],
  providers: [OverviewStore],
  templateUrl: './overview-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewPage {
  protected readonly store = inject(OverviewStore);
  protected readonly viewport = inject(Viewport);

  /** On a phone the six periods keep to one row. */
  protected readonly periods = computed(() => [
    { label: '1М', value: '1M' },
    { label: '3М', value: '3M' },
    { label: '6М', value: '6M' },
    { label: '1Р', value: '1Y' },
    { label: this.viewport.phone() ? 'YTD' : 'З початку року', value: 'YTD' },
    { label: 'Усе', value: 'ALL' },
  ]);
}
