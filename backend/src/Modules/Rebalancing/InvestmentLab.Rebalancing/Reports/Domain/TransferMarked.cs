namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record TransferMarked(
    Guid AccountId, string Reference, Guid FromSubPortfolioId, Guid ToSubPortfolioId, DateOnly ExecutedOn, Guid? InstrumentId, decimal Quantity, DateTimeOffset OccurredAt) : IDomainEvent;
