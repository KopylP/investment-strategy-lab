namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IReplaySignalWatch
{
    ReplaySignalSubject Subject { get; }

    IReadOnlyList<Guid> Instruments { get; }

    IReadOnlyList<Guid> ActiveInstruments { get; }

    bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings);
}
