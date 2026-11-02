namespace InvestmentLab.Signals.Contracts;

public interface IBuiltInSignalWatch
{
    string Subject { get; }

    bool FiresOn(DateOnly day, IReadOnlyList<SignalSubjectValueDto> readings);
}
