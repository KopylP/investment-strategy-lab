namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record StrategyReconfigured(StrategyId StrategyId, UserId OwnerId, int Version, DateTimeOffset OccurredAt, bool IsTrial) : IDomainEvent;
