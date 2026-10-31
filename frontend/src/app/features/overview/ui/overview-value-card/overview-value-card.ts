import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { ValueChart } from '../../ui/value-chart/value-chart';
import { Money } from '../../../../shared/ui/money/money';
import { SignedPercentPipe, DatePipe } from '../../../../shared/pipes/format-pipes';
import { DashboardDto } from '../../data-access/dashboard-api';
import { Viewport } from '../../../../shared/ui/viewport';
import { OverviewStore } from '../../data-access/overview-store';

@Component({
  selector: 'app-overview-value-card',
  imports: [
    FormsModule,
    SelectButton,
    Skeleton,
    Tooltip,
    TapTooltip,
    SignedValue,
    ValueChart,
    Money,
    SignedPercentPipe,
    DatePipe,
  ],
  templateUrl: './overview-value-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewValueCard {
  readonly dashboard = input.required<DashboardDto>();
  protected readonly store = inject(OverviewStore);

  protected readonly viewport = inject(Viewport);

  protected readonly periods = computed(() => [
    { label: '1М', value: '1M' },
    { label: '3М', value: '3M' },
    { label: '6М', value: '6M' },
    { label: '1Р', value: '1Y' },
    { label: this.viewport.phone() ? 'YTD' : 'З початку року', value: 'YTD' },
    { label: 'Усе', value: 'ALL' },
  ]);
}
