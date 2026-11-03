using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class ReplaySignals(IBuiltInSignalReplay signals, ISignalReplay userSignals) : IReplaySignals
{
    public IReplaySignalWatch Watch(string kind) => new ReplayedWatch(signals.Watch(kind));

    public async Task<IReplaySignalWatch?> WatchSignalAsync(Guid signalId, CancellationToken cancellationToken)
    {
        var watch = await userSignals.WatchAsync(signalId, cancellationToken);

        return watch is null ? null : new UserWatch(watch);
    }

    private sealed class UserWatch(ISignalReplayWatch watch) : IReplaySignalWatch
    {
        private readonly ISignalReplayWatch _watch = watch;

        public ReplaySignalSubject Subject => _watch.Subject switch
        {
            SignalReplaySubject.Instruments => ReplaySignalSubject.Instruments,
            SignalReplaySubject.Account => ReplaySignalSubject.Account,
            SignalReplaySubject.EachPosition => ReplaySignalSubject.EachPosition,
            var unknown => throw new InvalidOperationException($"Unknown signal subject '{unknown}'."),
        };

        public IReadOnlyList<Guid> Instruments => _watch.Instruments;
        public IReadOnlyList<Guid> ActiveInstruments => _watch.ActiveInstruments;

        public bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings) =>
            _watch.FiresOn(day, [.. readings.Select(reading => new SignalSubjectValueDto(reading.SubjectKey, reading.Value, reading.HeldSince, reading.UnitCost))]);
    }

    private sealed class ReplayedWatch(IBuiltInSignalWatch watch) : IReplaySignalWatch
    {
        private readonly IBuiltInSignalWatch _watch = watch;

        public IReadOnlyList<Guid> Instruments => [];
        public IReadOnlyList<Guid> ActiveInstruments => [];

        public ReplaySignalSubject Subject => _watch.Subject switch
        {
            BuiltInSignalSubject.VolatilityIndex => ReplaySignalSubject.VolatilityIndex,
            BuiltInSignalSubject.Account => ReplaySignalSubject.Account,
            BuiltInSignalSubject.EachPosition => ReplaySignalSubject.EachPosition,
            var unknown => throw new InvalidOperationException($"Unknown subject '{unknown}' of a built-in signal."),
        };

        public bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings) =>
            _watch.FiresOn(day, [.. readings.Select(reading => new SignalSubjectValueDto(reading.SubjectKey, reading.Value, reading.HeldSince, reading.UnitCost))]);
    }
}
