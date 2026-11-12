using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record TransactionSourceDto(string Kind, string? Reference)
{
    public static TransactionSourceDto Manual => new(nameof(TransactionSourceKind.Manual), null);

    public static TransactionSourceDto FromReport(string reference) => new(nameof(TransactionSourceKind.ReportAction), reference);

    public Result<TransactionSource> ToDomain()
    {
        var kind = EnumText.TryParse<TransactionSourceKind>(Kind, "transaction.source_unknown");
        if (kind.IsFailure)
            return kind.Error;

        return new TransactionSource(kind.Value, Reference);
    }
}
