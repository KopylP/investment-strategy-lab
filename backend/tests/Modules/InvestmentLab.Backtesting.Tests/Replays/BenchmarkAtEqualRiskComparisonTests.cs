using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Domain.Comparisons;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class BenchmarkAtEqualRiskComparisonTests
{
    private static readonly Guid Benchmark = Guid.NewGuid(), Cash = Guid.NewGuid();
    private static readonly DateOnly FirstDay = new(2020, 1, 1);
    private const int Days = 300;

    [Fact]
    public void A_calmer_strategy_is_compared_with_the_benchmark_diluted_by_cash_to_the_same_volatility()
    {
        var history = HistoryWith(strategySwing: 0.01, benchmarkSwing: 0.02);

        var line = new BenchmarkAtEqualRiskComparison().LineOf(history);

        var strategyVolatility = ReplaySeries.VolatilityOf(history.StrategyGrowth);
        ReplaySeries.VolatilityOf(line.Growth).ShouldBe(strategyVolatility, strategyVolatility * 0.02);
    }

    [Fact]
    public void A_riskier_strategy_is_compared_with_the_whole_benchmark_and_never_with_borrowed_money()
    {
        var history = HistoryWith(strategySwing: 0.04, benchmarkSwing: 0.02);

        var line = new BenchmarkAtEqualRiskComparison().LineOf(history);

        var benchmark = history.Prices.GrowthOf(Benchmark, history.FirstDay);
        line.Growth[^1].Growth.ShouldBe(benchmark[^1].Growth, 1e-9);
    }

    private static ReplayedHistory HistoryWith(double strategySwing, double benchmarkSwing)
    {
        var dates = Enumerable.Range(0, Days).Select(offset => FirstDay.AddDays(offset)).ToList();
        var prices = new PriceTable(new Dictionary<Guid, IReadOnlyList<PricedDay>>
        {
            [Benchmark] = Swinging(dates, benchmarkSwing),
            [Cash] = [.. dates.Select(date => new PricedDay(date, 100m))],
        });

        var strategy = Swinging(dates, strategySwing).Select(day => new GrowthDay(day.Date, (double)day.Price / 100d)).ToList();

        return new ReplayedHistory(prices, FirstDay: 0, SetupWithBenchmark(), strategy);
    }

    private static List<PricedDay> Swinging(List<DateOnly> dates, double swing)
    {
        var days = new List<PricedDay>();
        var price = 100d;

        foreach (var (date, index) in dates.Select((date, index) => (date, index)))
        {
            price *= index % 2 == 0 ? 1d + swing : 1d - swing;
            days.Add(new PricedDay(date, (decimal)price));
        }

        return days;
    }

    private static ReplaySetup SetupWithBenchmark() => new(
        StrategyId: Guid.NewGuid(),
        OwnerId: UserId.New(),
        Version: 1,
        CanBeReplayed: true,
        Universe: [Benchmark],
        CashInstrumentId: Cash,
        Benchmark: new ReplayBenchmark(Benchmark, "SXR8"),
        RebalanceCadence: ReplayCadence.Monthly,
        RiskReviewCadence: null,
        Signals: ReplaySignalTriggers.None,
        Currency: "EUR",
        Comparisons: ["benchmark-at-equal-risk"]);
}
