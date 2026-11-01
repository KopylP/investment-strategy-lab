using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class MoneyOutcomesTests
{
    private static readonly DateOnly Start = new(2020, 1, 31);

    [Fact]
    public void Contributions_grow_from_the_month_they_are_paid_in()
    {
        var growing = Enumerable.Range(0, 13).Select(month => month == 12 ? 2d : 1d).ToArray();

        var outcomes = MoneyOutcomes.Of(PointsOf(growing), MoneyPlan.TryOf(100m, 10m, 1).Value);

        outcomes.Periods.ShouldBe(1);
        outcomes.Typical!.Final.ShouldBe(440m);
    }

    [Fact]
    public void Worst_typical_and_best_come_from_different_starting_months()
    {
        var growth = new double[37];
        growth[0] = 1d;
        for (var month = 1; month < growth.Length; month++)
            growth[month] = growth[month - 1] * (month <= 12 ? 0.99 : 1.02);

        var outcomes = MoneyOutcomes.Of(PointsOf(growth), MoneyPlan.TryOf(1000m, 0m, 1).Value);

        outcomes.Periods.ShouldBe(25);
        outcomes.Worst!.Final.ShouldBeLessThan(outcomes.Typical!.Final);
        outcomes.Typical.Final.ShouldBeLessThan(outcomes.Best!.Final);
        outcomes.Worst.From.ShouldBe(Start);
    }

    [Fact]
    public void A_negative_plan_is_refused() =>
        MoneyPlan.TryOf(-1m, 0m, 5).Error.Code.ShouldBe("backtest.invalid_money_plan");

    private static List<ReplayPoint> PointsOf(params double[] growth) =>
        [.. growth.Select((value, month) => new ReplayPoint(Start.AddMonths(month), value, 0d))];
}
