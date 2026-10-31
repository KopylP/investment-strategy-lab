import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { Message } from 'primeng/message';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { DateTimePipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { DeclarationDialog } from '../../ui/declaration-dialog/declaration-dialog';
import { TaxesStore } from '../../data-access/taxes-store';

import { TaxDueCard } from '../../ui/tax-due-card/tax-due-card';

import { TaxSummaryTiles } from '../../ui/tax-summary-tiles/tax-summary-tiles';

import { TaxRealizationsCard } from '../../ui/tax-realizations-card/tax-realizations-card';

import { TaxDividendsCard } from '../../ui/tax-dividends-card/tax-dividends-card';

import { TaxOpenLotsCard } from '../../ui/tax-open-lots-card/tax-open-lots-card';

import { BrokerStatementsCard } from '../../ui/broker-statements-card/broker-statements-card';

import { LossCarryforwardsCard } from '../../ui/loss-carryforwards-card/loss-carryforwards-card';

@Component({
  selector: 'app-tax-year-page',
  imports: [
    LossCarryforwardsCard,
    BrokerStatementsCard,
    TaxOpenLotsCard,
    TaxDividendsCard,
    TaxRealizationsCard,
    TaxSummaryTiles,
    TaxDueCard,
    FormsModule,
    Message,
    SelectButton,
    Skeleton,
    Tag,
    PageHeader,
    EmptyState,
    DeclarationDialog,
    DateTimePipe,
  ],
  providers: [TaxesStore],
  templateUrl: './tax-year-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxYearPage {
  protected readonly store = inject(TaxesStore);
  private readonly confirm = inject(ConfirmationService);

  protected readonly yearOptions = computed(() =>
    this.store.years().map((year) => ({ label: String(year.year), value: year.year })),
  );
  protected confirmRemovePit8C(accountId: string): void {
    this.confirm.confirm({
      header: 'Прибрати суми PIT-8C?',
      message:
        'Виручка й витрати брокера в декларації знову рахуватимуться з ваших операцій, а не з його PIT-8C.',
      acceptLabel: 'Прибрати',
      rejectLabel: 'Скасувати',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.removePit8C(accountId),
    });
  }
}
