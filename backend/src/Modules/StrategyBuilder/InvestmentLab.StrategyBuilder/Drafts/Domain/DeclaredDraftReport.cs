namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DeclaredDraftReport(string Code, ReportTiming Schedule, IReadOnlyList<string> SignalKinds);
