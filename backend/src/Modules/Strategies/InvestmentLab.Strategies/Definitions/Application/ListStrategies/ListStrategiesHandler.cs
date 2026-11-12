using InvestmentLab.SharedKernel;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.ListStrategies;

public sealed class ListStrategiesHandler(IStrategyRepository strategies, IStrategyTypes types) : IHandler
{
    public async Task<IReadOnlyList<StrategyDto>> Handle(CancellationToken cancellationToken)
    {
        var all = await strategies.ListAsync(cancellationToken);
        var known = (await types.ListAsync(cancellationToken)).ToDictionary(type => type.Code);

        return [.. all.Select(strategy => strategy.ToDto(known.GetValueOrDefault(strategy.TypeCode)))];
    }
}
