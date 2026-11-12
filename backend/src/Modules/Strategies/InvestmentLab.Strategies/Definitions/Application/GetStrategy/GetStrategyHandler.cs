using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.GetStrategy;

public sealed class GetStrategyHandler(IStrategyRepository strategies, IStrategyTypes types) : IHandler
{
    public async Task<Result<StrategyDto>> Handle(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(strategyId);

        if (strategy.Version(version) is not { } shown)
            return StrategyErrors.VersionNotFound(strategyId, version!.Value);

        var type = await types.FindAsync(strategy.TypeCode, cancellationToken);

        return strategy.ToDto(type, shown);
    }
}
