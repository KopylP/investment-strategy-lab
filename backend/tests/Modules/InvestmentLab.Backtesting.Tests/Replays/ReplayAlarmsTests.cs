using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class ReplayAlarmsTests
{
    private static readonly Guid Fund = Guid.NewGuid(), Vix = Guid.NewGuid();
    private static readonly DateOnly Day = new(2008, 10, 10);

    [Fact]
    public void Each_held_position_is_read_at_its_price_since_its_first_purchase()
    {
        var signals = new RecordingSignals(ReplaySignalSubject.EachPosition, fires: false);
        var alarms = AlarmsOf(new ReplaySignalTriggers([], ["position-drawdown"], null), signals);
        var book = new ReplayBook(1000m);
        book.Buy(Fund, 100m, 10m, Day.AddDays(-30));
        book.Buy(Fund, 100m, 12m, Day.AddDays(-10));

        alarms.FiredOn(new ReplayMoment(Day, book, new Dictionary<Guid, decimal> { [Fund] = 8m }, 0.9d));

        var reading = signals.Seen.Single().Single();
        reading.Value.ShouldBe(8m);
        reading.HeldSince.ShouldBe(Day.AddDays(-30));
    }

    [Fact]
    public void The_volatility_index_is_read_at_its_level()
    {
        var signals = new RecordingSignals(ReplaySignalSubject.VolatilityIndex, fires: true);
        var alarms = AlarmsOf(new ReplaySignalTriggers([], ["volatility-index"], Vix), signals);

        var fired = alarms.FiredOn(new ReplayMoment(Day, new ReplayBook(1000m), new Dictionary<Guid, decimal> { [Vix] = 45m }, 1d));

        signals.Seen.Single().Single().Value.ShouldBe(45m);
        fired.ShouldBe(new FiredReports(Rebalance: false, RiskReview: true));
    }

    [Fact]
    public void A_signal_brings_only_the_reports_that_come_on_it()
    {
        var signals = new RecordingSignals(ReplaySignalSubject.Account, fires: true);
        var alarms = AlarmsOf(new ReplaySignalTriggers(["account-drawdown"], [], null), signals);

        var fired = alarms.FiredOn(new ReplayMoment(Day, new ReplayBook(1000m), new Dictionary<Guid, decimal>(), 0.6d));

        fired.ShouldBe(new FiredReports(Rebalance: true, RiskReview: false));
        signals.Seen.Single().Single().Value.ShouldBe(0.6m);
    }

    private sealed class RecordingSignals(ReplaySignalSubject subject, bool fires) : IReplaySignals
    {
        public List<IReadOnlyList<ReplaySignalReading>> Seen { get; } = [];

        public Task<IReplaySignalWatch?> WatchSignalAsync(Guid signalId, CancellationToken cancellationToken) =>
            Task.FromResult<IReplaySignalWatch?>(null);

        public IReplaySignalWatch Watch(string kind) => new Recording(this, subject, fires);

        private sealed class Recording(RecordingSignals owner, ReplaySignalSubject subject, bool fires) : IReplaySignalWatch
        {
            public IReadOnlyList<Guid> Instruments => [];
            public IReadOnlyList<Guid> ActiveInstruments => [];

            public ReplaySignalSubject Subject => subject;

            public bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings)
            {
                owner.Seen.Add(readings);

                return fires;
            }
        }
    }
    [Fact]
    public void A_user_signal_on_an_instrument_wakes_the_risk_review()
    {
        var id = Guid.NewGuid();
        var watch = new MarketWatch();
        var triggers = new ReplaySignalTriggers([], [], null, RiskReviewSignalIds: [id]);
        var watches = new ReplaySignalWatches(new Dictionary<string, IReplaySignalWatch>(), new Dictionary<Guid, IReplaySignalWatch> { [id] = watch });
        var alarms = new ReplayAlarms(triggers, [], watches);

        alarms.FiredOn(Moment(0)).RiskReview.ShouldBeFalse();
        alarms.FiredOn(Moment(1)).RiskReview.ShouldBeFalse();
        alarms.FiredOn(Moment(2)).RiskReview.ShouldBeTrue();
        watch.Readings.ShouldHaveSingleItem().SubjectKey.ShouldBe(Fund.ToString("N"));
    }

    [Fact]
    public void The_state_of_a_logic_signal_follows_its_alarm_and_rearm()
    {
        var id = Guid.NewGuid();
        var watch = new MarketWatch();
        var watches = new ReplaySignalWatches(new Dictionary<string, IReplaySignalWatch>(), new Dictionary<Guid, IReplaySignalWatch> { [id] = watch });
        var alarms = new ReplayAlarms(ReplaySignalTriggers.None, [id], watches);

        for (var day = 0; day < 6; day++)
        {
            var state = alarms.FiredOn(Moment(day)).States!.ShouldHaveSingleItem();

            state.SignalId.ShouldBe(id);
            state.ActiveFor.ShouldBe(day is >= 2 and <= 4 ? [Fund] : []);
        }
    }

    private static ReplayMoment Moment(int offset) =>
        new(Day.AddDays(offset), new ReplayBook(1000m), new Dictionary<Guid, decimal> { [Fund] = 90m }, 1d);

    private sealed class MarketWatch : IReplaySignalWatch
    {
        private int _days;
        public ReplaySignalSubject Subject => ReplaySignalSubject.Instruments;
        public IReadOnlyList<Guid> Instruments => [Fund];
        public IReadOnlyList<Guid> ActiveInstruments => _days is >= 3 and <= 5 ? [Fund] : [];
        public IReadOnlyList<ReplaySignalReading> Readings { get; private set; } = [];

        public bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings)
        {
            Readings = readings;
            _days++;

            return _days == 3;
        }
    }

    private static ReplayAlarms AlarmsOf(ReplaySignalTriggers triggers, IReplaySignals signals)
    {
        var builtIn = triggers.AllKinds.ToDictionary(kind => kind, signals.Watch);
        var watches = new ReplaySignalWatches(builtIn, new Dictionary<Guid, IReplaySignalWatch>());

        return new ReplayAlarms(triggers, [], watches);
    }
}
