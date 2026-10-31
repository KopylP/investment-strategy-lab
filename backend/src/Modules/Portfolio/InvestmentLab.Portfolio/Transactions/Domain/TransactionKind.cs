namespace InvestmentLab.Portfolio.Transactions.Domain;

public enum TransactionKind
{
    Buy,
    Sell,
    Contribution,
    Withdrawal,
    Fee,
    TaxPayment,
    Dividend,
    InternalTransfer,
    Reversal,
}
