import { TaxRegime } from '../../../../core/context/tax-regime';
import { TaxMoneyPipe } from '../../../../core/context/tax-money-pipe';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { MoneyPipe, SignedPercentPipe } from '../../../../shared/pipes/format-pipes';
import { DashboardDto } from '../../data-access/dashboard-api';
import { OverviewStore } from '../../data-access/overview-store';

@Component({
  selector: 'app-overview-metrics',
  imports: [TaxMoneyPipe, RouterLink, Tooltip, TapTooltip, SignedValue, MoneyPipe, SignedPercentPipe],
  templateUrl: './overview-metrics.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewMetrics {
  readonly dashboard = input.required<DashboardDto>();
  protected readonly store = inject(OverviewStore);
  protected readonly regime = inject(TaxRegime);
}
