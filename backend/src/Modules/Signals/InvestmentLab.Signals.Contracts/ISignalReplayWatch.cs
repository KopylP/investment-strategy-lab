namespace InvestmentLab.Signals.Contracts;

public interface ISignalReplayWatch
{
    string Subject { get; }
    IReadOnlyList<Guid> Instruments { get; }
    bool FiresOn(DateOnly day, IReadOnlyList<SignalSubjectValueDto> readings);
    IReadOnlyList<Guid> ActiveInstruments { get; }
}
