namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record RowOutcome(
    int Number,
    RowDecision Decision,
    string? Note,
    Guid? ExistingTransactionId,
    bool WillRecord,
    bool WillReverseExisting,
    string? Result);
