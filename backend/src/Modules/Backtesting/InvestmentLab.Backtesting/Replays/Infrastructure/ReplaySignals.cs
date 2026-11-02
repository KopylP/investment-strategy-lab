using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class ReplaySignals(IBuiltInSignalReplay signals) : IReplaySignals
{
    public IReplaySignalWatch Watch(string kind) => new ReplayedWatch(signals.Watch(kind));

    private sealed class ReplayedWatch(IBuiltInSignalWatch watch) : IReplaySignalWatch
    {
        private readonly IBuiltInSignalWatch _watch = watch;

        public ReplaySignalSubject Subject => _watch.Subject switch
        {
            BuiltInSignalSubject.VolatilityIndex => ReplaySignalSubject.VolatilityIndex,
            BuiltInSignalSubject.Account => ReplaySignalSubject.Account,
            BuiltInSignalSubject.EachPosition => ReplaySignalSubject.EachPosition,
            var unknown => throw new InvalidOperationException($"Unknown subject '{unknown}' of a built-in signal."),
        };

        public bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings) =>
            _watch.FiresOn(day, [.. readings.Select(reading => new SignalSubjectValueDto(reading.SubjectKey, reading.Value, reading.HeldSince))]);
    }
}
