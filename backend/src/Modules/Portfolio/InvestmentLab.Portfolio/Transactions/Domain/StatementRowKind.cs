namespace InvestmentLab.Portfolio.Transactions.Domain;

public enum StatementRowKind
{
    Buy,
    Sell,
    Contribution,
    Withdrawal,
    Dividend,

    Unsupported,
}
