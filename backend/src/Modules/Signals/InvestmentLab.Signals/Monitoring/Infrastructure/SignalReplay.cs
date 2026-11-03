using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class SignalReplay(ISignalRepository signals, IInstrumentGroups groups) : ISignalReplay
{
    public async Task<ISignalReplayWatch?> WatchAsync(Guid signalId, CancellationToken cancellationToken)
    {
        var signal = await signals.FindAsync(signalId, cancellationToken);
        if (signal is null)
            return null;

        var instruments = await InstrumentsOf(signal.Target, cancellationToken);
        var watch = new SignalWatch(signal.Condition);

        return new ReplayedWatch(SubjectOf(signal.Target), instruments, watch);
    }

    private async Task<IReadOnlyList<Guid>> InstrumentsOf(SignalTarget target, CancellationToken cancellationToken)
    {
        if (target.InstrumentId is { } instrumentId)
            return [instrumentId];
        if (target.Kind != SignalTargetKind.InstrumentGroup)
            return [];

        var members = await groups.MembersOfAsync(target.GroupName!, cancellationToken);

        return [.. members.Select(member => member.InstrumentId)];
    }

    private static string SubjectOf(SignalTarget target) => target.Kind switch
    {
        SignalTargetKind.Instrument or SignalTargetKind.InstrumentGroup => SignalReplaySubject.Instruments,
        SignalTargetKind.EachPosition => SignalReplaySubject.EachPosition,
        SignalTargetKind.Account or SignalTargetKind.SubPortfolio => SignalReplaySubject.Account,
        _ => throw new ArgumentOutOfRangeException(nameof(target)),
    };

    private sealed class ReplayedWatch(string subject, IReadOnlyList<Guid> instruments, SignalWatch watch) : ISignalReplayWatch
    {
        public string Subject => subject;
        public IReadOnlyList<Guid> Instruments => instruments;
        public IReadOnlyList<Guid> ActiveInstruments => ActiveInstrumentIds();

        private IReadOnlyList<Guid> ActiveInstrumentIds()
        {
            var instruments = watch.Alarmed.Select(key => Guid.TryParseExact(key, "N", out var id) ? (Guid?)id : null);

            return [.. instruments.OfType<Guid>()];
        }

        public bool FiresOn(DateOnly day, IReadOnlyList<SignalSubjectValueDto> readings)
        {
            var values = readings.Select(reading => new WatchedReading(reading.Key, reading.Value, reading.HeldSince, reading.UnitCost)).ToList();

            return watch.FiresOn(day, values);
        }
    }
}
