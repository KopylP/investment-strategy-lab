namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IReplaySignalWatch
{
    ReplaySignalSubject Subject { get; }

    bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings);
}
