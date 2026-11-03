namespace InvestmentLab.Strategies.Contracts;

public sealed record PublishRequest(Guid OwnerId, Guid? StrategyId, string Name, AuthoredSetupDto Setup);
