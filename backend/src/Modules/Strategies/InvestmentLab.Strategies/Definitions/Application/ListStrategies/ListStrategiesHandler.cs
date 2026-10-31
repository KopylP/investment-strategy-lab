using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.ListStrategies;

public sealed class ListStrategiesHandler(IStrategyRepository strategies)
{
    public async Task<IReadOnlyList<StrategyDto>> Handle(CancellationToken cancellationToken)
    {
        var all = await strategies.ListAsync(cancellationToken);

        return [.. all.Select(strategy => strategy.ToDto())];
    }
}
