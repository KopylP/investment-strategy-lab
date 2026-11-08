import { TranslocoPipe } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
import { InstrumentAbout } from '../../../../shared/ui/instrument-about';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { Money } from '../../../../shared/ui/money/money';
import { PositionChart } from '../../ui/position-chart';
import {
  MoneyPipe,
  PercentPipe,
  QuantityPipe,
  SignedPercentPipe,
  DatePipe,
} from '../../../../shared/pipes/format-pipes';
import { SleeveVm } from '../../data-access/holdings-store';
import { HoldingsStore } from '../../data-access/holdings-store';
import { Viewport } from '../../../../shared/ui/viewport';

@Component({
  selector: 'app-holdings-sleeve-card',
  imports: [TranslocoPipe, 
    Button,
    TableModule,
    Tooltip,
    TapTooltip,
    InstrumentAbout,
    SignedValue,
    Money,
    PositionChart,
    MoneyPipe,
    PercentPipe,
    QuantityPipe,
    SignedPercentPipe,
    DatePipe,
  ],
  templateUrl: './holdings-sleeve-card.html',
  host: { class: 'contents' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoldingsSleeveCard {
  readonly sleeve = input.required<SleeveVm>();
  protected readonly store = inject(HoldingsStore);
  protected readonly viewport = inject(Viewport);
}
