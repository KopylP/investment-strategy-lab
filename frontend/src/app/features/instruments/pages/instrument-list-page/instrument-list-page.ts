import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { instrumentKindLabel } from '../../../../shared/models/instrument-kind';
import {
  DatePipe,
  MoneyPipe,
  PercentPipe,
  SignedPercentPipe,
} from '../../../../shared/pipes/format-pipes';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { Sparkline } from '../../../../shared/ui/sparkline';
import {
  ChartPeriod,
  InstrumentFilter,
  InstrumentsStore,
} from '../../data-access/instruments-store';
import { PriceChart } from '../../../../shared/charts/price-chart';
import { AccountTrades } from '../../../../shared/data/account-trades';
import { LayoutService } from '../../../../core/layout/layout-service';
import { Viewport } from '../../../../shared/ui/viewport';
import { InstrumentAbout } from '../../../../shared/ui/instrument-about';

@Component({
  selector: 'app-instrument-list-page',
  imports: [TranslocoPipe, 
    FormsModule,
    Button,
    IconField,
    InputIcon,
    InputText,
    SelectButton,
    Skeleton,
    Tag,
    PageHeader,
    SignedValue,
    Sparkline,
    PriceChart,
    InstrumentAbout,
    MoneyPipe,
    DatePipe,
    PercentPipe,
    SignedPercentPipe,
  ],
  providers: [InstrumentsStore],
  templateUrl: './instrument-list-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstrumentListPage {
  protected readonly store = inject(InstrumentsStore);
  protected readonly trades = inject(AccountTrades);
  protected readonly viewport = inject(Viewport);
  private readonly layout = inject(LayoutService);

  protected readonly periods: { label: string; value: ChartPeriod }[] = [
    '1M',
    '3M',
    '6M',
    '1Y',
    '3Y',
  ].map((value) => ({
    label: translate(`instruments.period.${value.slice(-1)}`, { count: value.slice(0, -1) }),
    value: value as ChartPeriod,
  }));

  protected filters(
    counts: Record<InstrumentFilter, number>,
  ): { label: string; value: InstrumentFilter }[] {
    const all: { label: string; value: InstrumentFilter }[] = [
      { label: translate('instruments.filter.all', { count: counts.all }), value: 'all' },
      { label: translate('instruments.filter.mine', { count: counts.mine }), value: 'mine' },
      { label: `EU ETF ${counts.eu}`, value: 'eu' },
      { label: `US ETF ${counts.us}`, value: 'us' },
      { label: translate('instruments.filter.stocks', { count: counts.stocks }), value: 'stocks' },
      { label: translate('instruments.filter.other', { count: counts.other }), value: 'other' },
    ];

    return all.filter((filter) => filter.value === 'all' || counts[filter.value] > 0);
  }

  protected toggle(id: string): void {
    this.store.select(id);

    if (this.layout.isMobile()) window.scrollTo({ top: 0 });
  }

  protected kindLabel(kind: string): string {
    return instrumentKindLabel(kind);
  }
}
