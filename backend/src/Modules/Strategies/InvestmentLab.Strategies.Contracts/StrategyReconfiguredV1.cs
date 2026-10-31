namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyReconfiguredV1(Guid StrategyId, Guid OwnerId, int Version, DateTimeOffset ReconfiguredAt);
