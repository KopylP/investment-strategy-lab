namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed class UseStrategyScheduleHandler(OwnReportScheduleChange ownSchedule)
{
    public Task<Result<ReportScheduleDto>> Handle(Guid accountId, string code, CancellationToken cancellationToken) =>
        ownSchedule.ApplyAsync(accountId, code, (plan, now) => plan.UseStrategySchedule(code, now), cancellationToken);
}
