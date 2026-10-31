using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.MarkActions;

public sealed class FillMarker(IReportRepository reports, TimeProvider clock)
{
    public async Task<Result> MarkAsync(
        Guid reportId, Guid actionId, Func<RebalancingReport, ReportAction, DateTimeOffset, Result<MarkedFill>> mark, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        if (report.FindAction(ReportActionId.Of(actionId)) is not { } action)
            return ReportErrors.ActionNotFound(actionId);

        var marked = mark(report, action, clock.GetUtcNow());
        if (marked.IsFailure)
            return marked.Error;

        await reports.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public static DateOnly DayOf(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);
}
