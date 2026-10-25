using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.GetStrategy;

public sealed class GetStrategyHandler(IStrategyRepository strategies)
{
    public async Task<Result<StrategyDto>> Handle(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(strategyId);

        return strategy.Version(version) is { } shown
            ? strategy.ToDto(shown)
            : StrategyErrors.VersionNotFound(strategyId, version!.Value);
    }

    public async Task<IReadOnlyList<StrategyDto>> List(CancellationToken cancellationToken) =>
        [.. (await strategies.ListAsync(cancellationToken)).Select(strategy => strategy.ToDto())];
}
