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
import { symbolOf } from '../../../../shared/models/api';
import { DatePipe } from '../../../../shared/pipes/format-pipes';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { SignedValue } from '../../../../shared/ui/signed-value';
import { Viewport } from '../../../../shared/ui/viewport';
import { CashInput, TradeInput, TransactionsStore, TransactionVm } from '../../data-access/transactions-store';
import { CashDialog } from '../../ui/cash-dialog/cash-dialog';
import { TradeDialog } from '../../ui/trade-dialog/trade-dialog';

import { Money } from '../../../../shared/ui/money/money';
@Component({
  selector: 'app-ledger-page',
  imports: [Money,
    FormsModule, RouterLink, Button, IconField, InputIcon, InputText, Message, Select, Skeleton, TableModule, Tag, Tooltip,
    PageHeader, EmptyState, SignedValue, TradeDialog, CashDialog, DatePipe,
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
    { label: 'Угоди', value: 'trade' },
    { label: 'Рух коштів', value: 'cash' },
    { label: 'Перекази', value: 'InternalTransfer' },
    { label: 'Сторно', value: 'Reversal' },
  ];
  protected readonly instrumentOptions = computed(() => this.catalog.etfs().map((etf) => ({ label: `${symbolOf(etf)} · ${etf.name}`, value: etf.id })));
  protected readonly subOptions = computed(() =>
    (this.store.account()?.subPortfolios ?? []).filter((sub) => sub.status === 'Active').map((sub) => ({ label: sub.name, value: sub.id })),
  );
  protected readonly currency = computed(() => this.store.account()?.baseCurrency ?? 'EUR');

  protected readonly visibleTransactions = computed(() => {
    const filter = this.kindFilter();
    const text = this.search().trim().toLowerCase();
    return this.store.transactions().filter((transaction) => {
      const kindOk =
        !filter ||
        (filter === 'trade' && (transaction.kind === 'Buy' || transaction.kind === 'Sell')) ||
        (filter === 'cash' && ['Contribution', 'Withdrawal', 'Fee', 'TaxPayment', 'Dividend'].includes(transaction.kind)) ||
        transaction.kind === filter;
      return kindOk && (!text || `${transaction.kindLabel} ${transaction.description} ${transaction.allocation}`.toLowerCase().includes(text));
    });
  });

  protected async recordTrade(input: TradeInput): Promise<void> {
    if (await this.store.recordTrade(input)) this.tradeVisible.set(false);
  }

  protected async recordCash(input: CashInput): Promise<void> {
    if (await this.store.recordCash(input)) this.cashVisible.set(false);
  }

  protected confirmReverse(transaction: TransactionVm): void {
    this.confirm.confirm({
      header: 'Сторнувати операцію?',
      message: 'Оригінал лишиться в журналі, а сторно скасує його вплив на позиції, кеш і податки.',
      icon: 'pi pi-replay',
      acceptLabel: 'Сторнувати',
      rejectLabel: 'Ні',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => void this.store.reverse(transaction, 'скасовано користувачем'),
    });
  }
}
