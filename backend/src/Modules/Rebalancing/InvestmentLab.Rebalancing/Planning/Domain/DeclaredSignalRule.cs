namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record DeclaredSignalRule(UserId OwnerId, Guid SignalId, Guid AccountId, string ReportCode);
