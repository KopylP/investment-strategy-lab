using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class BuiltInSignalReplay : IBuiltInSignalReplay
{
    public IBuiltInSignalWatch Watch(string kind)
    {
        var subject = SubjectOf(BuiltInSignalRules.TargetOf(kind));
        var watch = new SignalWatch(BuiltInSignalRules.ConditionOf(kind));

        return new ReplayedWatch(subject, watch);
    }

    private static string SubjectOf(SignalTargetKind target) => target switch
    {
        SignalTargetKind.Instrument => BuiltInSignalSubject.VolatilityIndex,
        SignalTargetKind.Account => BuiltInSignalSubject.Account,
        SignalTargetKind.EachPosition => BuiltInSignalSubject.EachPosition,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "A built-in signal has no such target."),
    };

    private sealed class ReplayedWatch(string subject, SignalWatch watch) : IBuiltInSignalWatch
    {
        public string Subject => subject;

        public bool FiresOn(DateOnly day, IReadOnlyList<SignalSubjectValueDto> readings) =>
            watch.FiresOn(day, [.. readings.Select(reading => new WatchedReading(reading.Key, reading.Value, reading.HeldSince))]);
    }
}
