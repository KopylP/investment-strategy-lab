namespace InvestmentLab.Backtesting.Contracts;

public interface IBacktestRequests
{
    Task EnsureAsync(Guid strategyId, int version, CancellationToken cancellationToken);
}
