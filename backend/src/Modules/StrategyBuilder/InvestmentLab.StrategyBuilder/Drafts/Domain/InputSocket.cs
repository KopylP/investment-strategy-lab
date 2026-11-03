namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record InputSocket(string Name, SocketType Type, bool Required, bool Many);
