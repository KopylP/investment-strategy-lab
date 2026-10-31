namespace InvestmentLab.Rebalancing.Reports.Application.Shared;

public sealed record MatchedOperationDto(
    Guid TransactionId,
    DateOnly OccurredOn,
    string Kind,
    Guid? InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    string Source);
