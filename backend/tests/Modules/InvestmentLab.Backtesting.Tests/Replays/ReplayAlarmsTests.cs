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
        var alarms = new ReplayAlarms(new ReplaySignalTriggers([], ["position-drawdown"], null), signals);
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
        var alarms = new ReplayAlarms(new ReplaySignalTriggers([], ["volatility-index"], Vix), signals);

        var fired = alarms.FiredOn(new ReplayMoment(Day, new ReplayBook(1000m), new Dictionary<Guid, decimal> { [Vix] = 45m }, 1d));

        signals.Seen.Single().Single().Value.ShouldBe(45m);
        fired.ShouldBe(new FiredReports(Rebalance: false, RiskReview: true));
    }

    [Fact]
    public void A_signal_brings_only_the_reports_that_come_on_it()
    {
        var signals = new RecordingSignals(ReplaySignalSubject.Account, fires: true);
        var alarms = new ReplayAlarms(new ReplaySignalTriggers(["account-drawdown"], [], null), signals);

        var fired = alarms.FiredOn(new ReplayMoment(Day, new ReplayBook(1000m), new Dictionary<Guid, decimal>(), 0.6d));

        fired.ShouldBe(new FiredReports(Rebalance: true, RiskReview: false));
        signals.Seen.Single().Single().Value.ShouldBe(0.6m);
    }

    private sealed class RecordingSignals(ReplaySignalSubject subject, bool fires) : IReplaySignals
    {
        public List<IReadOnlyList<ReplaySignalReading>> Seen { get; } = [];

        public IReplaySignalWatch Watch(string kind) => new Recording(this, subject, fires);

        private sealed class Recording(RecordingSignals owner, ReplaySignalSubject subject, bool fires) : IReplaySignalWatch
        {
            public ReplaySignalSubject Subject => subject;

            public bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings)
            {
                owner.Seen.Add(readings);

                return fires;
            }
        }
    }
}
