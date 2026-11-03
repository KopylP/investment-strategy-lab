using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Strategies.Definitions.Application;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyRepository(StrategiesDbContext db) : IStrategyRepository
{
    public Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken) =>
        db.Strategies.SingleOrDefaultAsync(strategy => strategy.Id == strategyId, cancellationToken);

    public async Task<IReadOnlyList<Strategy>> ListAsync(CancellationToken cancellationToken) =>
        await db.Strategies.Where(strategy => strategy.Status != StrategyStatus.Trial).OrderBy(strategy => strategy.CreatedAt).ToListAsync(cancellationToken);

    public Task<bool> NameTakenAsync(UserId ownerId, string name, CancellationToken cancellationToken) =>
        db.Strategies.IgnoreQueryFilters(
            [ModuleDbContext.OwnerFilter]).AnyAsync(strategy => strategy.OwnerId == ownerId && strategy.Name == name && strategy.Status != StrategyStatus.Trial,
            cancellationToken);

    public void Add(Strategy strategy) => db.Strategies.Add(strategy);

    public void Remove(Strategy strategy) => db.Strategies.Remove(strategy);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
