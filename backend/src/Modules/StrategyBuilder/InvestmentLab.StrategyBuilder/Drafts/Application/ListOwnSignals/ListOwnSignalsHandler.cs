using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.ListOwnSignals;

public sealed record OwnSignalDto(Guid Id, string Name, bool IsMarket, string Target);

public sealed class ListOwnSignalsHandler(IOwnSignals signals) : IHandler
{
    public async Task<IReadOnlyList<OwnSignalDto>> Handle(CancellationToken cancellationToken)
    {
        var all = await signals.AllAsync(cancellationToken);

        return [.. all.Select(signal => new OwnSignalDto(signal.Id, signal.Name, signal.IsMarket, signal.Target))];
    }
}
