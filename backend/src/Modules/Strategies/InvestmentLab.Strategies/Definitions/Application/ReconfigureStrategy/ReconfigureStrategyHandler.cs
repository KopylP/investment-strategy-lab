using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.ReconfigureStrategy;

public sealed class ReconfigureStrategyHandler(IStrategyRepository strategies, StrategySetupFactory setups, TimeProvider clock)
{
    public async Task<Result<StrategyDto>> Handle(Guid strategyId, StrategySetupInput command, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(strategyId);

        var setup = await setups.CreateAsync(strategy.TypeCode, command, cancellationToken);
        if (setup.IsFailure)
            return setup.Error;

        var now = clock.GetUtcNow();
        var version = strategy.Reconfigure(setup.Value, now);
        if (version.IsFailure)
            return version.Error;

        strategies.Publish(new StrategyReconfiguredV1(strategy.Id.Value, strategy.OwnerId.Value, version.Value.Number, now), now);
        await strategies.SaveChangesAsync(cancellationToken);
        return strategy.ToDto();
    }
}
