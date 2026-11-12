using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Rebalancing.Planning.Application.Shared;

namespace InvestmentLab.Rebalancing.Planning.Application.EnsureDeclaredSignalRules;

public sealed class DeclaredSignalRulesJob(EnsureDeclaredSignalRulesHandler handler) : IScheduledJob
{
    public static IJobSchedule Schedule => ReportSchedules.Check;

    public Task RunAsync(CancellationToken cancellationToken) => handler.Handle(cancellationToken);
}
