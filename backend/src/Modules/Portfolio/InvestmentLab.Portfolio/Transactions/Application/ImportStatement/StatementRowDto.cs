namespace InvestmentLab.Portfolio.Transactions.Application.ImportStatement;

public sealed record StatementRowDto(
    int Number, DateOnly OccurredOn, string Kind, string RawType, string? Ticker, decimal? Quantity, decimal? Price, decimal Amount, string Currency,
    decimal AmountInAccountCurrency, string Decision, string? Note, bool WillRecord, bool WillReverseExisting, string? Result);
