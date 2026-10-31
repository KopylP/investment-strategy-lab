namespace InvestmentLab.Rebalancing.Reports.Application.LoadOperations;

public sealed record MatchedOperationDto(Guid TransactionId, DateOnly OccurredOn, string Kind, Guid? InstrumentId, decimal Quantity, decimal Price, decimal Fee, string Source);
