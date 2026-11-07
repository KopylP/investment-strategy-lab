using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class HistoryShortfallTests
{
    private const int HistoryBars = 5;
    private const int TableDays = 400;
    private static readonly DateOnly FirstDay = new(2015, 1, 1);
    private static readonly DateOnly RequestedStart = FirstDay.AddDays(10);

    [Fact]
    public void A_benchmark_warmed_up_late_limits_the_start_and_is_named()
    {
        var benchmark = Guid.NewGuid();
        var funds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var history = funds.ToDictionary(fund => fund, _ => QuotesFrom(0));
        history[benchmark] = QuotesFrom(296);

        var shortfall = new PriceTable(history).ShortfallAt(SetupOf(funds, benchmark), RequestedStart);

        shortfall.ShouldNotBeNull();
        shortfall.EarliestStart.ShouldBe(FirstDay.AddDays(300));
        var limit = shortfall.Limit.ShouldBeOfType<ReferenceHistoryLimit>();
        limit.InstrumentId.ShouldBe(benchmark);
        limit.WarmedFrom.ShouldBe(FirstDay.AddDays(300));
    }

    [Fact]
    public void Too_few_warmed_funds_limit_the_start_and_list_the_funds_still_warming_up()
    {
        var benchmark = Guid.NewGuid();
        var funds = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToList();
        var history = new Dictionary<Guid, IReadOnlyList<PricedDay>> { [benchmark] = QuotesFrom(0) };
        int[] firstQuotes = [0, 0, 0, 100, 200, 210, 220, 230];
        for (var index = 0; index < funds.Count; index++)
            history[funds[index]] = QuotesFrom(firstQuotes[index]);

        var shortfall = new PriceTable(history).ShortfallAt(SetupOf(funds, benchmark), RequestedStart);

        shortfall.ShouldNotBeNull();
        shortfall.EarliestStart.ShouldBe(FirstDay.AddDays(104));
        var limit = shortfall.Limit.ShouldBeOfType<FundsHistoryLimit>();
        limit.Warmed.ShouldBe(3);
        limit.Needed.ShouldBe(4);
        limit.NotWarmed.ShouldBe([.. funds.Skip(3)], ignoreOrder: true);
    }

    private static IReadOnlyList<PricedDay> QuotesFrom(int firstDay) =>
        [.. Enumerable.Range(firstDay, TableDays - firstDay).Select(day => new PricedDay(FirstDay.AddDays(day), 100m + day))];

    private static ReplaySetup SetupOf(IReadOnlyList<Guid> funds, Guid benchmark) => new(
        StrategyId: Guid.NewGuid(),
        OwnerId: UserId.New(),
        Version: 1,
        CanBeReplayed: true,
        Universe: funds,
        CashInstrumentId: null,
        Benchmark: new ReplayBenchmark(benchmark, "Benchmark"),
        RebalanceCadence: ReplayCadence.Monthly,
        RiskReviewCadence: null,
        Signals: new ReplaySignalTriggers([], [], null),
        Currency: "EUR",
        Comparisons: [],
        HistoryBars: HistoryBars,
        Experiment: new ReplayExperiment(RequestedStart, FirstDay.AddDays(TableDays - 1), 10_000m, new TradingCosts()));
}
