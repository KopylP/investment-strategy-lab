using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Evaluation.Application;
using InvestmentLab.Strategies.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Evaluation.Infrastructure;

internal sealed class StrategyReader(StrategiesDbContext db) : IStrategyReader
{
    public Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken) =>
        db.Strategies.AsNoTracking().SingleOrDefaultAsync(strategy => strategy.Id == strategyId, cancellationToken);
}
