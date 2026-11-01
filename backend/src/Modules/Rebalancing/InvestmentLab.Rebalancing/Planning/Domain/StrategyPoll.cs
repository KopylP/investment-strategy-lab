namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyPoll(StrategyRunState State, string? Answer, string? ErrorCode, string? ErrorMessage);
