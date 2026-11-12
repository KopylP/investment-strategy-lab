using InvestmentLab.SharedKernel;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.ArchiveStrategy;

public sealed class ArchiveStrategyHandler(IStrategyRepository strategies) : IHandler
{
    public async Task<Result> Handle(Guid strategyId, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(strategyId);

        strategy.Archive();
        await strategies.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
