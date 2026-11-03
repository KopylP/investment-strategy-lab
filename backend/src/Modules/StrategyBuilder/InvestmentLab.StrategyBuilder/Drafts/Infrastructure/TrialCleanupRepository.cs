using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class TrialCleanupRepository(StrategyBuilderDbContext db) : ITrialCleanupRepository
{
    public Task<TrialCleanup?> FindAsync(Guid cleanupId, CancellationToken cancellationToken) =>
        db.TrialCleanups.FirstOrDefaultAsync(cleanup => cleanup.Id == cleanupId, cancellationToken);

    public async Task<IReadOnlyList<Guid>> DueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.TrialCleanups.Where(cleanup => cleanup.NextAttemptAt <= now).OrderBy(cleanup => cleanup.NextAttemptAt).Select(cleanup => cleanup.Id).ToListAsync(cancellationToken);

    public void Add(TrialCleanup cleanup) => db.TrialCleanups.Add(cleanup);
    public void Remove(TrialCleanup cleanup) => db.TrialCleanups.Remove(cleanup);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
