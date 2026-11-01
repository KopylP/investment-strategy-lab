using InvestmentLab.BuildingBlocks.Scheduling;

namespace InvestmentLab.Rebalancing.Planning.Application.EnsureDeclaredSignalRules;

public sealed class DeclaredSignalRulesJob(EnsureDeclaredSignalRulesHandler handler) : IScheduledJob
{
    public Task RunAsync(CancellationToken cancellationToken) => handler.Handle(cancellationToken);
}
