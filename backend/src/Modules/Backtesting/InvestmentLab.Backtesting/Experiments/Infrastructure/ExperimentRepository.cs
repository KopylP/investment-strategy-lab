using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Backtesting.Experiments.Infrastructure;

internal sealed class ExperimentRepository(BacktestingDbContext db) : IExperimentRepository
{
    public Task<ResearchExperiment?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Experiments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Guid?> NextAsync(CancellationToken cancellationToken)
    {
        var unfinished = db.Experiments.Where(item => item.Status == ExperimentStatus.Queued || item.Status == ExperimentStatus.Running);
        var oldestFirst = unfinished.OrderBy(item => item.RequestedAt);

        return oldestFirst.Select(item => (Guid?)item.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(ResearchExperiment experiment) => db.Experiments.Add(experiment);

    public Task RemoveOfStrategyAsync(Guid strategyId, CancellationToken cancellationToken) =>
        db.Experiments.Where(experiment => experiment.StrategyId == strategyId).ExecuteDeleteAsync(cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
