using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class BacktestRepository(BacktestingDbContext db) : IBacktestRepository
{
    public Task<Backtest?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Backtests.FirstOrDefaultAsync(backtest => backtest.Id == id, cancellationToken);

    public Task<Backtest?> FindAsync(Guid strategyId, int strategyVersion, CancellationToken cancellationToken) =>
        db.Backtests.FirstOrDefaultAsync(backtest => backtest.StrategyId == strategyId && backtest.StrategyVersion == strategyVersion, cancellationToken);

    public async Task<Guid?> NextToRunAsync(CancellationToken cancellationToken) =>
        await db.Backtests
            .Where(backtest => backtest.Status == BacktestStatus.Queued || backtest.Status == BacktestStatus.Running)
            .OrderBy(backtest => backtest.RequestedAt)
            .Select(backtest => (Guid?)backtest.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Backtest>> AllAsync(CancellationToken cancellationToken) =>
        await db.Backtests.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Backtest>> OfStrategyAsync(Guid strategyId, CancellationToken cancellationToken) =>
        await db.Backtests.Where(backtest => backtest.StrategyId == strategyId).ToListAsync(cancellationToken);

    public void Add(Backtest backtest) => db.Backtests.Add(backtest);

    public void Remove(Backtest backtest) => db.Backtests.Remove(backtest);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
