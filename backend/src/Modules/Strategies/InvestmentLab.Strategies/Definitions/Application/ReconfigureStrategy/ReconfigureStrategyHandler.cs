using InvestmentLab.SharedKernel;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.ReconfigureStrategy;

public sealed class ReconfigureStrategyHandler(IStrategyRepository strategies, IStrategyTypes types, StrategySetupFactory setups, TimeProvider clock) : IHandler
{
    public async Task<Result<StrategyDto>> Handle(Guid strategyId, StrategySetupDraft command, CancellationToken cancellationToken)
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

        await strategies.SaveChangesAsync(cancellationToken);

        var type = await types.FindAsync(strategy.TypeCode, cancellationToken);

        return strategy.ToDto(type);
    }
}
