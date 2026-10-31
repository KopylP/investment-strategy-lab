using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.GetStrategy;

public sealed class GetStrategyHandler(IStrategyRepository strategies)
{
    public async Task<Result<StrategyDto>> Handle(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(strategyId);

        if (strategy.Version(version) is not { } shown)
            return StrategyErrors.VersionNotFound(strategyId, version!.Value);

        return strategy.ToDto(shown);
    }
}
