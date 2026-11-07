namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class DraftReports
{
    public static IReadOnlyList<DraftReport> DefaultsOf(IReadOnlyList<DeclaredDraftReport> declared) =>
        [.. declared.Select(report => new DraftReport(report.Code, report.Schedule, report.SignalKinds, []))];

    public static Result<IReadOnlyList<DraftReport>> Rescheduled(IReadOnlyList<DraftReport> reports, string code, ReportTimingPatch patch)
    {
        if (reports.FirstOrDefault(report => report.Code == code) is not { } current)
            return DraftErrors.ReportUnknown(code);

        var schedule = patch.Over(current.Schedule);
        if (schedule.IsFailure)
            return schedule.Error;

        var changed = current with { Schedule = schedule.Value };
        IReadOnlyList<DraftReport> rescheduled = [.. reports.Select(report => report.Code == code ? changed : report)];

        return Result<IReadOnlyList<DraftReport>>.Success(rescheduled);
    }
}
