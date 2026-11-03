namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyDeletedV1(Guid StrategyId, Guid OwnerId, DateTimeOffset DeletedAt);
