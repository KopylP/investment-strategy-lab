using InvestmentLab.SharedKernel;
using InvestmentLab.Rebalancing.Planning.Application.Shared;

namespace InvestmentLab.Rebalancing.Planning.Application.UseStrategySchedule;

public sealed class UseStrategyScheduleHandler(OwnReportScheduleChange ownSchedule) : IHandler
{
    public Task<Result<ReportScheduleDto>> Handle(Guid accountId, string code, CancellationToken cancellationToken) =>
        ownSchedule.ApplyAsync(accountId, code, (plan, now) => plan.UseStrategySchedule(code, now), cancellationToken);
}
