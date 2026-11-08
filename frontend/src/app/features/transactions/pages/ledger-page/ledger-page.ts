import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { Button } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import { InstrumentCatalog } from '../../../../core/context/instrument-catalog';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { Viewport } from '../../../../shared/ui/viewport';
import {
  NewCashMovement,
  NewTrade,
  TransactionsStore,
  TransactionVm,
} from '../../data-access/transactions-store';
import { CashDialog } from '../../ui/cash-dialog/cash-dialog';
import { TradeDialog } from '../../ui/trade-dialog/trade-dialog';

import { Money } from '../../../../shared/ui/money/money';
import { TapTooltip } from '../../../../shared/ui/tap-tooltip';
@Component({
  selector: 'app-ledger-page',
  imports: [
    TranslocoPipe,
    Money,
    FormsModule,
    RouterLink,
    Button,
    IconField,
    InputIcon,
    InputText,
    Message,
    Select,
    Skeleton,
    TableModule,
    Tag,
    Tooltip,
    TapTooltip,
    PageHeader,
    EmptyState,
    SignedValue,
    TradeDialog,
    CashDialog,
    DatePipe,
  ],
  providers: [TransactionsStore],
  templateUrl: './ledger-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LedgerPage {
  protected readonly store = inject(TransactionsStore);
  private readonly catalog = inject(InstrumentCatalog);
  private readonly confirm = inject(ConfirmationService);
  protected readonly viewport = inject(Viewport);

  protected readonly tradeVisible = signal(false);
  protected readonly cashVisible = signal(false);
  protected readonly kindFilter = signal<string | null>(null);
  protected readonly search = signal('');

  protected readonly kindOptions = [
    { label: translate('transactions.ledger.filter.trades'), value: 'trade' },
    { label: translate('transactions.ledger.filter.cash'), value: 'cash' },
    { label: translate('transactions.ledger.filter.contributions'), value: 'Contribution' },
    { label: translate('transactions.ledger.filter.withdrawals'), value: 'Withdrawal' },
    { label: translate('transactions.ledger.filter.transfers'), value: 'InternalTransfer' },
    { label: translate('transactions.ledger.filter.reversals'), value: 'Reversal' },
  ];
  protected readonly instrumentOptions = computed(() =>
    this.catalog.etfs().map((etf) => ({ label: `${etf.label} · ${etf.name}`, value: etf.id })),
  );
  protected readonly subOptions = computed(() =>
    (this.store.account()?.subPortfolios ?? [])
      .filter((sub) => sub.status === 'Active')
      .map((sub) => ({ label: sub.name, value: sub.id })),
  );
  protected readonly currency = computed(() => this.store.account()?.baseCurrency ?? 'EUR');

  protected readonly visibleTransactions = computed(() => {
    const filter = this.kindFilter();
    const text = this.search().trim().toLowerCase();

    return this.store.transactions().filter((transaction) => {
      const kindOk =
        !filter ||
        (filter === 'trade' && (transaction.kind === 'Buy' || transaction.kind === 'Sell')) ||
        (filter === 'cash' &&
          ['Contribution', 'Withdrawal', 'Fee', 'TaxPayment', 'Dividend'].includes(
            transaction.kind,
          )) ||
        transaction.kind === filter;

      return (
        kindOk &&
        (!text ||
          `${transaction.kindLabel} ${transaction.description} ${transaction.allocation}`
            .toLowerCase()
            .includes(text))
      );
    });
  });

  protected async recordTrade(trade: NewTrade): Promise<void> {
    if (await this.store.recordTrade(trade)) this.tradeVisible.set(false);
  }

  protected async recordCash(movement: NewCashMovement): Promise<void> {
    if (await this.store.recordCash(movement)) this.cashVisible.set(false);
  }

  protected confirmReverse(transaction: TransactionVm): void {
    this.confirm.confirm({
      header: translate('transactions.ledger.reverseQuestion'),
      message: translate('transactions.ledger.reverseHint'),
      icon: 'pi pi-replay',
      acceptLabel: translate('transactions.ledger.reverse'),
      rejectLabel: translate('common.cancel'),
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () =>
        void this.store.reverse(transaction, translate('transactions.ledger.cancelledByUser')),
    });
  }
}
