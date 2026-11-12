using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Shared;

public sealed class FillMarker(IReportRepository reports, TimeProvider clock)
{
    public async Task<Result> MarkAsync(
        Guid reportId,
        Guid actionId,
        Func<RebalancingReport, ReportAction, DateTimeOffset, Result<MarkedFill>> mark,
        CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        if (report.FindAction(ReportActionId.Of(actionId)) is not { } action)
            return ReportErrors.ActionNotFound(actionId);

        var marked = mark(report, action, clock.GetUtcNow());
        if (marked.IsFailure)
            return marked.Error;

        var saved = await reports.TrySaveChangesAsync(cancellationToken);

        return saved ? Result.Success() : ReportErrors.ChangedMeanwhile(reportId);
    }

    public static DateOnly DayOf(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);
}
