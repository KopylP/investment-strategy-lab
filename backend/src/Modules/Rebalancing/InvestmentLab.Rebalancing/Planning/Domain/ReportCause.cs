namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record ReportCause(Guid SignalTriggerId, string Text);
