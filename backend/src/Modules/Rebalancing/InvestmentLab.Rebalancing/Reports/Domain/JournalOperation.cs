namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record JournalOperation(Guid TransactionId, DateOnly OccurredOn, JournalOperationKind Kind, Guid? InstrumentId, decimal Quantity, decimal Price, decimal Fee);
