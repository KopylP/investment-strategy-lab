using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.ChangeDraftReportSchedule;

public sealed record ChangeDraftReportScheduleCommand(
    int Revision,
    string ReportCode,
    string Frequency,
    int? Day = null,
    string? Weekday = null,
    string? Time = null,
    int? Minute = null)
{
    public ReportTimingPatch Schedule => new(Frequency, Day, Weekday, Time, Minute);
}

public sealed class ChangeDraftReportScheduleHandler(IDraftRepository drafts, DraftDescriber describer, TimeProvider clock)
{
    public async Task<Result<DraftDto>> Handle(Guid draftId, ChangeDraftReportScheduleCommand command, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        var reports = DraftReports.Rescheduled(draft.Reports, command.ReportCode, command.Schedule);
        if (reports.IsFailure)
            return reports.Error;

        var change = new DraftChange(
            Revision: command.Revision,
            Name: null,
            Description: null,
            Setup: null,
            Graph: null,
            Reports: reports.Value);
        var changed = draft.Change(change, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await drafts.SaveChangesAsync(cancellationToken);

        return await describer.DescribeAsync(draft, cancellationToken);
    }
}
