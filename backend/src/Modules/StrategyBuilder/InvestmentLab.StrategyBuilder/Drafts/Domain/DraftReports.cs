namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class DraftReports
{
    public static IReadOnlyList<DraftReport> DefaultsOf(IReadOnlyList<DeclaredDraftReport> declared) =>
        [.. declared.Select(report => new DraftReport(report.Code, report.Schedule, report.SignalKinds, []))];
}
