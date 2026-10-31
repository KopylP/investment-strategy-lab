using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.MarkActions;

public sealed class CloseReportHandler(IReportRepository reports, TimeProvider clock)
{
    public async Task<Result> Handle(Guid reportId, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var closed = report.Close(clock.GetUtcNow());
        if (closed.IsFailure)
            return closed;

        await reports.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
