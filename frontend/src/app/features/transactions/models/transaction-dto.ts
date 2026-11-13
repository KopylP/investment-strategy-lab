import { ApiSchema } from '../../../core/api/api-schema';

export type TransactionKind =
  | 'Buy'
  | 'Sell'
  | 'Contribution'
  | 'Withdrawal'
  | 'Fee'
  | 'TaxPayment'
  | 'Dividend'
  | 'InternalTransfer'
  | 'Reversal';
export type TransactionSource = 'ReportAction' | 'Manual' | 'Reconciliation' | 'Import';

export type TransactionDto = Omit<ApiSchema<'TransactionDto'>, 'kind' | 'source'> & {
  kind: TransactionKind;
  source: TransactionSource;
};

export type RecordTradeBody = ApiSchema<'RecordTradeCommand'>;
export type RecordCashMovementBody = ApiSchema<'RecordCashMovementCommand'>;
export type ReverseTransactionBody = ApiSchema<'ReverseTransactionCommand'>;
export type RecordedTransaction = ApiSchema<'RecordedTransaction'>;
