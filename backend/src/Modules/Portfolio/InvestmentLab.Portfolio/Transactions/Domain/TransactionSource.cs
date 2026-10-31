namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record TransactionSource(TransactionSourceKind Kind, string? Reference)
{
    public static TransactionSource Manual => new(TransactionSourceKind.Manual, null);
}
