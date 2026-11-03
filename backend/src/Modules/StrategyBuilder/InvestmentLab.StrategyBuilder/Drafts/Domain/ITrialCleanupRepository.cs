namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface ITrialCleanupRepository
{
    Task<TrialCleanup?> FindAsync(Guid cleanupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> DueAsync(DateTimeOffset now, CancellationToken cancellationToken);
    void Add(TrialCleanup cleanup);
    void Remove(TrialCleanup cleanup);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
