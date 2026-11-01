namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SignalRuleChanges(IReadOnlyList<DeclaredSignalRule> ToAdd, IReadOnlyList<SignalReportRule> ToRemove);
