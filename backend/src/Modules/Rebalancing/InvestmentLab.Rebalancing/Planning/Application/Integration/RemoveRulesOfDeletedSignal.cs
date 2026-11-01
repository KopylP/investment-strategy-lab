using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.Integration;

internal sealed class RemoveRulesOfDeletedSignal(ISignalReportRuleRepository rules) : IIntegrationEventHandler<SignalDeletedV1>
{
    public async Task Handle(SignalDeletedV1 deleted, CancellationToken cancellationToken)
    {
        var attached = await rules.OfOwnedSignalWithDismissedAsync(deleted.SignalId, deleted.OwnerId, cancellationToken);
        foreach (var rule in attached)
            rules.Remove(rule);

        await rules.SaveChangesAsync(cancellationToken);
    }
}
