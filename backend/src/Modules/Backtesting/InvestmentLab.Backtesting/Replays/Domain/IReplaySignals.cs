namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IReplaySignals
{
    IReplaySignalWatch Watch(string kind);

    Task<IReplaySignalWatch?> WatchSignalAsync(Guid signalId, CancellationToken cancellationToken);
}
