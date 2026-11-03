namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IBacktestRepository
{
    Task<Backtest?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<Backtest?> FindAsync(Guid strategyId, int strategyVersion, CancellationToken cancellationToken);

    Task<Guid?> NextToRunAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Backtest>> AllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Backtest>> OfStrategyAsync(Guid strategyId, CancellationToken cancellationToken);

    void Add(Backtest backtest);

    void Remove(Backtest backtest);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
