using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.SkipAction;

public sealed record SkipActionCommand(string Reason);

public sealed class SkipActionHandler(IReportRepository reports, TimeProvider clock)
{
    public async Task<Result> Handle(Guid reportId, Guid actionId, SkipActionCommand command, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var skipped = report.Skip(ReportActionId.Of(actionId), command.Reason, clock.GetUtcNow());
        if (skipped.IsFailure)
            return skipped;

        await reports.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
