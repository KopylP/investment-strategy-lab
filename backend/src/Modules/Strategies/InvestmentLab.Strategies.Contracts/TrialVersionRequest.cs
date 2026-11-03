namespace InvestmentLab.Strategies.Contracts;

public sealed record TrialVersionRequest(Guid OwnerId, Guid? TrialStrategyId, AuthoredSetupDto Setup);
