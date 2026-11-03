namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DraftReport(string Code, ReportTiming Schedule, IReadOnlyList<string> SignalKinds, IReadOnlyList<Guid> SignalIds);
