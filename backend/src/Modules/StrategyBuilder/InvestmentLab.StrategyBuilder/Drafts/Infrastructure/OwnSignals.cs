using InvestmentLab.Signals.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class OwnSignals(ISignalDirectory directory) : IOwnSignals
{
    public async Task<IReadOnlyList<OwnSignal>> AllAsync(CancellationToken cancellationToken)
    {
        var signals = await directory.GetSignalsAsync(cancellationToken);

        return [.. signals.Select(signal => new OwnSignal(signal.Id, signal.Name, signal.IsMarketSignal, signal.Target))];
    }
}
