namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record StrategyDeleted(StrategyId StrategyId, UserId OwnerId, DateTimeOffset OccurredAt) : IDomainEvent;
