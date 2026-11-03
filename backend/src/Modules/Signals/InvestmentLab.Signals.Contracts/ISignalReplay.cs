namespace InvestmentLab.Signals.Contracts;

public interface ISignalReplay
{
    Task<ISignalReplayWatch?> WatchAsync(Guid signalId, CancellationToken cancellationToken);
}
