namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record DeclaredDraftReport(
    string Code,
    string Name,
    string Description,
    ReportTiming Schedule,
    IReadOnlyList<string> SignalKinds);
