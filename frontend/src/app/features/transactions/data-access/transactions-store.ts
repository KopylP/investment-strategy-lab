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

export interface TradeInput {
  instrumentId: string;
  side: 'Buy' | 'Sell';
  quantity: number;
  price: number;
  fee: number;
  executedOn: string;
  subPortfolioId: string | null;
}

export interface CashInput {
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

  async recordTrade(input: TradeInput): Promise<boolean> {
    return this.command(
      `trades`,
      {
        instrumentId: input.instrumentId,
        side: input.side,
        quantity: input.quantity,
        price: input.price,
        fee: input.fee,
        executedOn: input.executedOn,
        allocations: input.subPortfolioId
          ? [{ subPortfolioId: input.subPortfolioId, quantity: input.quantity }]
          : null,
      },
      'Угоду записано',
    );
  }

  async recordCash(input: CashInput): Promise<boolean> {
    return this.command(
      'cash-movements',
      {
        kind: input.kind,
        amount: input.amount,
        occurredOn: input.occurredOn,
        allocations: input.subPortfolioId
          ? [{ subPortfolioId: input.subPortfolioId, amount: input.amount }]
          : null,
        instrumentId: input.instrumentId,
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
      const result = await this.api.post<{ warnings?: string[] }>(
        `/api/portfolio/accounts/${accountId}/${path}`,
        body,
      );
      this.messages.add({
        severity: 'success',
        summary: success,
        detail: result?.warnings?.join(' ') || undefined,
      });
      this.journalResource.reload();
      return true;
    } catch (error) {
      this.messages.add({
        severity: 'error',
        summary: 'Не записано',
        detail: toProblemMessage(error),
        life: 8000,
      });
      return false;
    } finally {
      this.busy.set(false);
    }
  }

  private toVm(
    transaction: TransactionDto,
    currency: string,
    sub: (id: string) => string,
  ): TransactionVm {
    const { trade, cash, transfer } = transaction;
    let description = '';
    let allocation = '';
    let amount: number | null = null;
    if (trade) {
      description = `${this.instruments.symbol(trade.instrumentId)} · ${formatQuantity(trade.quantity)} × ${formatMoney(trade.price, currency)}${trade.fee ? ` · комісія ${formatMoney(trade.fee, currency)}` : ''}${settled(trade)}`;
      allocation = trade.allocations
        .map((part) => `${sub(part.subPortfolioId)}: ${formatQuantity(part.quantity)}`)
        .join(', ');
      amount = (trade.side === 'Buy' ? -1 : 1) * trade.quantity * trade.price - trade.fee;
    } else if (cash) {
      description =
        `${cash.instrumentId ? this.instruments.symbol(cash.instrumentId) : ''}${settled(cash)}`.replace(
          /^ · /,
          '',
        );
      allocation = cash.allocations
        .map((part) => `${sub(part.subPortfolioId)}: ${formatMoney(part.amount, currency)}`)
        .join(', ');
      amount =
        cash.kind === 'Contribution' || cash.kind === 'Dividend' ? cash.amount : -cash.amount;
    } else if (transfer) {
      description = transfer.instrumentId
        ? `${this.instruments.symbol(transfer.instrumentId)} · ${formatQuantity(transfer.quantity)}`
        : formatMoney(transfer.amount, currency);
      allocation = `${sub(transfer.fromSubPortfolioId)} → ${sub(transfer.toSubPortfolioId)}`;
    } else if (transaction.kind === 'Reversal') {
      description = transaction.reversalReason ?? '';
    }
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

function settled(entry: {
  settledAmount?: number | null;
  settledCurrency?: string | null;
}): string {
  return entry.settledAmount && entry.settledCurrency
    ? ` · у брокера ${formatMoney(entry.settledAmount, entry.settledCurrency)}`
    : '';
}
