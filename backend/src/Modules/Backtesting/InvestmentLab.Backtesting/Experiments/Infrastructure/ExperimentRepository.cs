using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Backtesting.Experiments.Infrastructure;

internal sealed class ExperimentRepository(BacktestingDbContext db) : IExperimentRepository
{
    public Task<ResearchExperiment?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Experiments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Guid?> NextAsync(CancellationToken cancellationToken) =>
        UnfinishedOldestFirst().Select(item => (Guid?)item.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<UnfinishedExperiment>> UnfinishedInOrderAsync(CancellationToken cancellationToken) =>
        await UnfinishedOldestFirst().Select(item => new UnfinishedExperiment(item.Id, item.Status)).ToListAsync(cancellationToken);

    public void Add(ResearchExperiment experiment) => db.Experiments.Add(experiment);

    public Task RemoveOfStrategyAsync(Guid strategyId, CancellationToken cancellationToken) =>
        db.Experiments.Where(experiment => experiment.StrategyId == strategyId).ExecuteDeleteAsync(cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private IQueryable<ResearchExperiment> UnfinishedOldestFirst()
    {
        var unfinished = db.Experiments.Where(item => item.Status == ExperimentStatus.Queued || item.Status == ExperimentStatus.Running);

        return unfinished.OrderBy(item => item.RequestedAt);
    }
}
