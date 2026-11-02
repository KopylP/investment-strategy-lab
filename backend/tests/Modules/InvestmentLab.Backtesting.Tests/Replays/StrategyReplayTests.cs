using InvestmentLab.Backtesting.Replays.Application.RunBacktest;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class StrategyReplayTests
{
    private static readonly Guid Fund = Guid.NewGuid(), Other = Guid.NewGuid(), Benchmark = Guid.NewGuid();
    private static readonly DateOnly FirstDay = new(2020, 1, 1);
    private const int Days = 520;

    [Fact]
    public async Task Works_with_whatever_answers_a_strategy_gives_and_keeps_monthly_points()
    {
        var answers = new FakeAnswers();

        var replayed = await ReplayAsync(answers, riskReview: null);

        var result = replayed.Value;
        result.Series.Select(series => series.Kind).ShouldBe([ReplaySeriesKind.Strategy, ReplaySeriesKind.Benchmark, ReplaySeriesKind.EqualWeight]);
        var points = result.SeriesOf(ReplaySeriesKind.Strategy).Points;
        points.Select(point => (point.Date.Year, point.Date.Month)).Distinct().Count().ShouldBe(points.Count);
        answers.Decisions.ShouldBe(points.Count);
        answers.Reviews.ShouldBe(0);
    }

    [Fact]
    public async Task A_strategy_without_a_risk_review_is_not_asked_about_risk_and_one_with_it_is()
    {
        var answers = new FakeAnswers();

        await ReplayAsync(answers, ReplayCadence.Weekly);

        answers.Reviews.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task A_decision_made_at_the_close_is_executed_at_the_next_close()
    {
        var replayed = await ReplayAsync(new FakeAnswers(), riskReview: null, jumpOnDayAfterStart: true);

        var strategy = replayed.Value.SeriesOf(ReplaySeriesKind.Strategy).Points[^1].Growth;
        var benchmark = replayed.Value.SeriesOf(ReplaySeriesKind.Benchmark).Points[^1].Growth;
        benchmark.ShouldBe(2d, 0.001);
        strategy.ShouldBeLessThan(1.01);
    }

    [Fact]
    public async Task An_exit_signal_sells_the_position_into_the_cash_instrument()
    {
        var answers = new FakeAnswers { ExitFund = true };

        var replayed = await ReplayAsync(answers, ReplayCadence.Weekly);

        replayed.Value.RiskExits.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Too_short_a_history_is_refused()
    {
        var replay = new StrategyReplay(new FakePrices(days: 100, jump: false), new FakeAnswers(), new FakeTrades(), new FakeSignals());

        var replayed = await replay.RunAsync(SetupOf(riskReview: null), TestContext.Current.CancellationToken);

        replayed.Error.Code.ShouldBe("backtest.not_enough_history");
    }

    [Fact]
    public async Task A_failing_strategy_fails_the_replay()
    {
        var replay = new StrategyReplay(new FakePrices(Days, jump: false), new FakeAnswers { Fails = true }, new FakeTrades(), new FakeSignals());

        var replayed = await replay.RunAsync(SetupOf(riskReview: null), TestContext.Current.CancellationToken);

        replayed.Error.Code.ShouldBe("backtest.strategy_failed");
    }

    [Fact]
    public async Task A_risk_review_that_comes_on_a_signal_is_also_made_on_the_days_the_signal_fires()
    {
        var answers = new FakeAnswers();
        var signals = new FakeSignals(firesEvery: 30);
        var triggers = new ReplaySignalTriggers([], [FallKind], VolatilityIndexInstrumentId: null);

        var replayed = await ReplayAsync(answers, riskReview: null, signals: signals, triggers: triggers);

        answers.Reviews.ShouldBeGreaterThan(0);
        replayed.Value.SignalReports.ShouldBeGreaterThan(0);
        signals.Readings.ShouldAllBe(readings => readings.Count == 1 && readings[0].SubjectKey == "account");
    }

    [Fact]
    public async Task A_signal_no_report_comes_on_is_not_watched()
    {
        var signals = new FakeSignals(firesEvery: 30);

        var replayed = await ReplayAsync(new FakeAnswers(), ReplayCadence.Weekly, signals: signals);

        signals.Readings.ShouldBeEmpty();
        replayed.Value.SignalReports.ShouldBe(0);
    }

    private const string FallKind = "account-drawdown";

    private static Task<Result<ReplayResult>> ReplayAsync(
        FakeAnswers answers,
        ReplayCadence? riskReview,
        bool jumpOnDayAfterStart = false,
        FakeSignals? signals = null,
        ReplaySignalTriggers? triggers = null)
    {
        var replay = new StrategyReplay(new FakePrices(Days, jumpOnDayAfterStart), answers, new FakeTrades(), signals ?? new FakeSignals());

        return replay.RunAsync(SetupOf(riskReview, triggers), TestContext.Current.CancellationToken);
    }

    private static ReplaySetup SetupOf(ReplayCadence? riskReview, ReplaySignalTriggers? triggers = null) => new(
        StrategyId: Guid.NewGuid(),
        OwnerId: UserId.New(),
        Version: 1,
        CanBeReplayed: true,
        Universe: [Fund],
        CashInstrumentId: Other,
        BenchmarkInstrumentId: Benchmark,
        RebalanceCadence: ReplayCadence.Monthly,
        RiskReviewCadence: riskReview,
        Signals: triggers ?? ReplaySignalTriggers.None,
        Currency: "EUR");

    private sealed class FakePrices(int days, bool jump) : IPriceTables
    {
        public Task<PriceTable> LoadAsync(ReplaySetup setup, CancellationToken cancellationToken)
        {
            var dates = Enumerable.Range(0, days).Select(offset => FirstDay.AddDays(offset)).ToList();
            var jumpFrom = 260;

            List<PricedDay> Rising() => [.. dates.Select((date, index) => new PricedDay(date, 100m + index))];
            List<PricedDay> Jumping() => [.. dates.Select((date, index) => new PricedDay(date, index >= jumpFrom ? 200m : 100m))];

            var history = new Dictionary<Guid, IReadOnlyList<PricedDay>>
            {
                [Fund] = jump ? Jumping() : Rising(),
                [Other] = Rising(),
                [Benchmark] = jump ? Jumping() : Rising(),
            };

            return Task.FromResult(new PriceTable(history));
        }
    }

    private sealed class FakeAnswers : IStrategyAnswers
    {
        public int Decisions { get; private set; }
        public int Reviews { get; private set; }
        public bool ExitFund { get; init; }
        public bool Fails { get; init; }

        public Task<Result<ReplayDecision>> DecideAsync(ReplaySetup setup, ReplayBook book, DateOnly day, CancellationToken cancellationToken)
        {
            Decisions++;

            Result<ReplayDecision> answer = Fails
                ? Error.Validation("backtest.strategy_failed", "no")
                : new ReplayDecision(true, 1m, [new ReplayTarget(Fund, 1m)]);

            return Task.FromResult(answer);
        }

        public Task<Result<IReadOnlyList<Guid>>> ExitsAsync(ReplaySetup setup, ReplayBook book, DateOnly day, CancellationToken cancellationToken)
        {
            Reviews++;
            IReadOnlyList<Guid> exits = ExitFund ? [Fund] : [];

            return Task.FromResult<Result<IReadOnlyList<Guid>>>(Result<IReadOnlyList<Guid>>.Success(exits));
        }
    }

    private sealed class FakeTrades : ITargetTrades
    {
        public IReadOnlyList<ReplayTrade> TradesFor(ReplayDecision decision, ReplaySetup setup, ReplayBook book, IReadOnlyDictionary<Guid, decimal> prices) =>
            book.Cash > 0m ? [new ReplayTrade(Fund, false, 0m, book.Cash)] : [];
    }

    private sealed class FakeSignals(int firesEvery = 0) : IReplaySignals
    {
        public List<IReadOnlyList<ReplaySignalReading>> Readings { get; } = [];

        public IReplaySignalWatch Watch(string kind) => new AccountWatch(this, firesEvery);

        private sealed class AccountWatch(FakeSignals owner, int firesEvery) : IReplaySignalWatch
        {
            private int _days;

            public ReplaySignalSubject Subject => ReplaySignalSubject.Account;

            public bool FiresOn(DateOnly day, IReadOnlyList<ReplaySignalReading> readings)
            {
                owner.Readings.Add(readings);
                _days++;

                return firesEvery > 0 && _days % firesEvery == 0;
            }
        }
    }
}
