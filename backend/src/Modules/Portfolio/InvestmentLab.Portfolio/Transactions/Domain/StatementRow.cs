namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record StatementRow(
    int Number,
    DateTimeOffset OccurredAt,
    StatementRowKind Kind,
    string RawType,
    string? Ticker,
    decimal? Quantity,
    decimal? Price,
    decimal Amount,
    string Currency,
    string Identity)
{
    public DateOnly OccurredOn => DateOnly.FromDateTime(OccurredAt.UtcDateTime);
    public bool IsTrade => Kind is StatementRowKind.Buy or StatementRowKind.Sell;
    public bool NeedsInstrument => Kind is StatementRowKind.Buy or StatementRowKind.Sell or StatementRowKind.Dividend;
}
