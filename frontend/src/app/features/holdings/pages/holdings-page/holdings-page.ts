import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';

import {} from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { Viewport } from '../../../../shared/ui/viewport';
import { HoldingsStore } from '../../data-access/holdings-store';

import { HoldingsSummaryCard } from '../../ui/holdings-summary-card/holdings-summary-card';

import { HoldingsTiles } from '../../ui/holdings-tiles/holdings-tiles';

import { HoldingsExposureCard } from '../../ui/holdings-exposure-card/holdings-exposure-card';

import { HoldingsSleeveCard } from '../../ui/holdings-sleeve-card/holdings-sleeve-card';

@Component({
  selector: 'app-holdings-page',
  imports: [
    HoldingsSleeveCard,
    HoldingsTiles,
    HoldingsExposureCard,
    HoldingsSummaryCard,
    FormsModule,
    RouterLink,
    Button,
    DatePicker,
    Message,
    Skeleton,
    PageHeader,
    EmptyState,
  ],
  providers: [HoldingsStore],
  templateUrl: './holdings-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoldingsPage {
  protected readonly store = inject(HoldingsStore);
  protected readonly viewport = inject(Viewport);
  protected readonly today = new Date();
}
