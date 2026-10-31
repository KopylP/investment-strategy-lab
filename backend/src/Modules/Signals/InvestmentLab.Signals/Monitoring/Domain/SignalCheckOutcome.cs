namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SignalCheckOutcome(
    Guid? TriggerId,
    IReadOnlyList<FiredAlarm> Fired,
    IReadOnlyList<SignalOccurrence> Recovered,
    IReadOnlyList<SignalOccurrence> Gone,
    int StillActive);
