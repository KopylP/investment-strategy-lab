using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.DeleteTrialStrategy;

public sealed class DeleteTrialStrategyHandler(IStrategyRepository strategies, TimeProvider clock) : IHandler
{
    public async Task<Result> Handle(Guid strategyId, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return Result.Success();

        var deleted = strategy.Delete(clock.GetUtcNow());
        if (deleted.IsFailure)
            return deleted;

        strategies.Remove(strategy);
        await strategies.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
