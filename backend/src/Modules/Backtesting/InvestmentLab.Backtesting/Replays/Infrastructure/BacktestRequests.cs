using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.Backtesting.Replays.Application.EnsureBacktest;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class BacktestRequests(EnsureBacktestHandler ensure) : IBacktestRequests
{
    public async Task EnsureAsync(Guid strategyId, int version, CancellationToken cancellationToken) =>
        await ensure.Handle(strategyId, version, cancellationToken);
}
