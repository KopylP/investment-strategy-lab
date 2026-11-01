namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IReplayedStrategies
{
    Task<ReplaySetup?> FindAsync(Guid strategyId, int? version, CancellationToken cancellationToken);
}
