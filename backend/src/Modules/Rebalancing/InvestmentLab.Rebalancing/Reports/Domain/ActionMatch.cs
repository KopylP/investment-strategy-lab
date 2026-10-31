namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ActionMatch(ReportActionId ActionId, IReadOnlyList<JournalOperation> Operations, bool Completes);
