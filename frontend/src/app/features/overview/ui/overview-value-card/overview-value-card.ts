import { TranslocoPipe, translate } from '@jsverse/transloco';
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
    TranslocoPipe,
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
    { label: translate('overview.value.period.1M'), value: '1M' },
    { label: translate('overview.value.period.3M'), value: '3M' },
    { label: translate('overview.value.period.6M'), value: '6M' },
    { label: translate('overview.value.period.1Y'), value: '1Y' },
    { label: this.viewport.phone() ? 'YTD' : translate('overview.value.period.ytd'), value: 'YTD' },
    { label: translate('overview.value.period.all'), value: 'ALL' },
  ]);
}
