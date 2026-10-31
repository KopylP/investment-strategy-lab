namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyInfo(Guid StrategyId, int Version, string Code, string EngineVersion);
