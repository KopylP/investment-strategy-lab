import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ApiClient, toProblemMessage } from '../../../core/api/api-client';
import { AccountContext } from '../../../core/context/account-context';
import { InstrumentCatalog } from '../../../core/context/instrument-catalog';
import { formatMoney, formatQuantity } from '../../../shared/pipes/format';

export interface TransactionDto {
  id: string;
  kind:
    | 'Buy'
    | 'Sell'
    | 'Contribution'
    | 'Withdrawal'
    | 'Fee'
    | 'TaxPayment'
    | 'Dividend'
    | 'InternalTransfer'
    | 'Reversal';
  occurredOn: string;
  recordedAt: string;
  source: 'ReportAction' | 'Manual' | 'Reconciliation' | 'Import';
  sourceReference: string | null;
  isReversed: boolean;
  trade: {
    instrumentId: string;
    side: string;
    quantity: number;
    price: number;
    fee: number;
    allocations: { subPortfolioId: string; quantity: number }[];
    settledAmount?: number | null;
    settledCurrency?: string | null;
  } | null;
  cash: {
    kind: string;
    amount: number;
    instrumentId: string | null;
    allocations: { subPortfolioId: string; amount: number }[];
    settledAmount?: number | null;
    settledCurrency?: string | null;
  } | null;
  transfer: {
    fromSubPortfolioId: string;
    toSubPortfolioId: string;
    instrumentId: string | null;
    quantity: number | null;
    amount: number | null;
  } | null;
  reversalOf: string | null;
  reversalReason: string | null;
}

export interface TransactionVm {
  id: string;
  occurredOn: string;
  kind: TransactionDto['kind'];
  kindLabel: string;
  severity: 'success' | 'danger' | 'info' | 'warn' | 'secondary' | 'contrast';
  description: string;
  allocation: string;
  amount: number | null;
  sourceLabel: string;
  isReversed: boolean;
  canReverse: boolean;
  reversalReason: string | null;
}

export interface NewTrade {
  instrumentId: string;
  side: 'Buy' | 'Sell';
  quantity: number;
  price: number;
  fee: number;
  executedOn: string;
  subPortfolioId: string | null;
}

export interface NewCashMovement {
  kind: 'Contribution' | 'Withdrawal' | 'Fee' | 'TaxPayment' | 'Dividend';
  amount: number;
  occurredOn: string;
  subPortfolioId: string | null;
  instrumentId: string | null;
}

const kinds: Record<
  TransactionDto['kind'],
  { label: string; severity: TransactionVm['severity'] }
> = {
  Buy: { label: 'Купівля', severity: 'success' },
  Sell: { label: 'Продаж', severity: 'danger' },
  Contribution: { label: 'Внесок', severity: 'info' },
  Withdrawal: { label: 'Вивід', severity: 'warn' },
  Fee: { label: 'Комісія', severity: 'warn' },
  TaxPayment: { label: 'Податок', severity: 'warn' },
  Dividend: { label: 'Дивіденд', severity: 'success' },
  InternalTransfer: { label: 'Переказ', severity: 'secondary' },
  Reversal: { label: 'Сторно', severity: 'contrast' },
};

const sources: Record<TransactionDto['source'], string> = {
  ReportAction: 'з рапорту',
  Manual: 'вручну',
  Reconciliation: 'звірка',
  Import: 'імпорт',
};

@Injectable()
export class TransactionsStore {
  private readonly api = inject(ApiClient);
  private readonly messages = inject(MessageService);
  private readonly accounts = inject(AccountContext);
  private readonly instruments = inject(InstrumentCatalog);

  private readonly journalResource = resource({
    params: () => this.accounts.currentId(),
    loader: ({ params: accountId }) =>
      this.api.get<TransactionDto[]>(`/api/portfolio/accounts/${accountId}/transactions`),
  });

  readonly account = this.accounts.current;
  readonly isLoading = this.journalResource.isLoading;
  readonly loadError = this.journalResource.error;
  readonly busy = signal(false);
  readonly transactions = computed(() => {
    const account = this.account();
    const names = new Map(account?.subPortfolios.map((sub) => [sub.id, sub.name]) ?? []);
    const currency = account?.baseCurrency ?? 'EUR';

    return [...(this.journalResource.value() ?? [])]
      .sort(
        (left, right) =>
          right.occurredOn.localeCompare(left.occurredOn) ||
          right.recordedAt.localeCompare(left.recordedAt),
      )
      .map((transaction) => this.toVm(transaction, currency, (id) => names.get(id) ?? '—'));
  });

  async recordTrade(trade: NewTrade): Promise<boolean> {
    return this.command(
      `trades`,
      {
        instrumentId: trade.instrumentId,
        side: trade.side,
        quantity: trade.quantity,
        price: trade.price,
        fee: trade.fee,
        executedOn: trade.executedOn,
        allocations: trade.subPortfolioId
          ? [{ subPortfolioId: trade.subPortfolioId, quantity: trade.quantity }]
          : null,
      },
      'Угоду записано',
    );
  }

  async recordCash(movement: NewCashMovement): Promise<boolean> {
    return this.command(
      'cash-movements',
      {
        kind: movement.kind,
        amount: movement.amount,
        occurredOn: movement.occurredOn,
        allocations: movement.subPortfolioId
          ? [{ subPortfolioId: movement.subPortfolioId, amount: movement.amount }]
          : null,
        instrumentId: movement.instrumentId,
      },
      'Рух коштів записано',
    );
  }

  reverse(transaction: TransactionVm, reason: string): Promise<boolean> {
    return this.command(
      `transactions/${transaction.id}/reverse`,
      { reason },
      'Операцію сторновано',
    );
  }

  private async command(path: string, body: unknown, success: string): Promise<boolean> {
    const accountId = this.accounts.currentId();
    if (!accountId) return false;

    this.busy.set(true);

    try {
      const url = `/api/portfolio/accounts/${accountId}/${path}`;
      const result = await this.api.post<{ warnings?: string[] }>(url, body);

      this.reportRecorded(success, result?.warnings);
      this.journalResource.reload();

      return true;
    } catch (error) {
      this.reportNotRecorded(error);

      return false;
    } finally {
      this.busy.set(false);
    }
  }

  private reportRecorded(summary: string, warnings?: string[]): void {
    this.messages.add({
      severity: 'success',
      summary,
      detail: warnings?.join(' ') || undefined,
    });
  }

  private reportNotRecorded(error: unknown): void {
    this.messages.add({
      severity: 'error',
      summary: 'Не записано',
      detail: toProblemMessage(error),
      life: 8000,
    });
  }

  private detailsOf(
    transaction: TransactionDto,
    currency: string,
    sub: (id: string) => string,
  ): TransactionDetails {
    const { trade, cash, transfer } = transaction;

    if (trade) return this.tradeDetails(trade, currency, sub);

    if (cash) return this.cashDetails(cash, currency, sub);

    if (transfer) return this.transferDetails(transfer, currency, sub);

    const description = transaction.kind === 'Reversal' ? (transaction.reversalReason ?? '') : '';

    return { description, allocation: '', amount: null };
  }

  private tradeDetails(
    trade: NonNullable<TransactionDto['trade']>,
    currency: string,
    sub: (id: string) => string,
  ): TransactionDetails {
    const symbol = this.instruments.symbol(trade.instrumentId);
    const size = `${formatQuantity(trade.quantity)} × ${formatMoney(trade.price, currency)}`;
    const fee = trade.fee ? ` · комісія ${formatMoney(trade.fee, currency)}` : '';
    const allocation = trade.allocations
      .map((part) => `${sub(part.subPortfolioId)}: ${formatQuantity(part.quantity)}`)
      .join(', ');
    const direction = trade.side === 'Buy' ? -1 : 1;

    return {
      description: `${symbol} · ${size}${fee}${settled(trade)}`,
      allocation,
      amount: direction * trade.quantity * trade.price - trade.fee,
    };
  }

  private cashDetails(
    cash: NonNullable<TransactionDto['cash']>,
    currency: string,
    sub: (id: string) => string,
  ): TransactionDetails {
    const symbol = cash.instrumentId ? this.instruments.symbol(cash.instrumentId) : '';
    const allocation = cash.allocations
      .map((part) => `${sub(part.subPortfolioId)}: ${formatMoney(part.amount, currency)}`)
      .join(', ');
    const isIncome = cash.kind === 'Contribution' || cash.kind === 'Dividend';

    return {
      description: `${symbol}${settled(cash)}`.replace(/^ · /, ''),
      allocation,
      amount: isIncome ? cash.amount : -cash.amount,
    };
  }

  private transferDetails(
    transfer: NonNullable<TransactionDto['transfer']>,
    currency: string,
    sub: (id: string) => string,
  ): TransactionDetails {
    const description = transfer.instrumentId
      ? `${this.instruments.symbol(transfer.instrumentId)} · ${formatQuantity(transfer.quantity)}`
      : formatMoney(transfer.amount, currency);

    return {
      description,
      allocation: `${sub(transfer.fromSubPortfolioId)} → ${sub(transfer.toSubPortfolioId)}`,
      amount: null,
    };
  }

  private toVm(
    transaction: TransactionDto,
    currency: string,
    sub: (id: string) => string,
  ): TransactionVm {
    const { description, allocation, amount } = this.detailsOf(transaction, currency, sub);

    return {
      id: transaction.id,
      occurredOn: transaction.occurredOn,
      kind: transaction.kind,
      kindLabel: kinds[transaction.kind].label,
      severity: kinds[transaction.kind].severity,
      description,
      allocation,
      amount,
      sourceLabel: sources[transaction.source],
      isReversed: transaction.isReversed,
      canReverse:
        !transaction.isReversed &&
        transaction.kind !== 'Reversal' &&
        transaction.source !== 'ReportAction',
      reversalReason: transaction.reversalReason,
    };
  }
}

interface TransactionDetails {
  description: string;
  allocation: string;
  amount: number | null;
}

function settled(entry: {
  settledAmount?: number | null;
  settledCurrency?: string | null;
}): string {
  return entry.settledAmount && entry.settledCurrency
    ? ` · у брокера ${formatMoney(entry.settledAmount, entry.settledCurrency)}`
    : '';
}
