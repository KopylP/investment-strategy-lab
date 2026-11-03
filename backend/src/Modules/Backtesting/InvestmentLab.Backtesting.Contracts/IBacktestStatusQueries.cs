namespace InvestmentLab.Backtesting.Contracts;

public interface IBacktestStatusQueries
{
    Task<BacktestStatusDto?> GetStatusAsync(Guid strategyId, int version, CancellationToken cancellationToken);
}
