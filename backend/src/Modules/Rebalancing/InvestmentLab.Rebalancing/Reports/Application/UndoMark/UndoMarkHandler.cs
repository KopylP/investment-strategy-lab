using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.UndoMark;

public sealed class UndoMarkHandler(IReportRepository reports, TimeProvider clock)
{
    public async Task<Result> Handle(Guid reportId, Guid actionId, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var undone = report.UndoMark(ReportActionId.Of(actionId), clock.GetUtcNow());
        if (undone.IsFailure)
            return undone.Error;

        await reports.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
