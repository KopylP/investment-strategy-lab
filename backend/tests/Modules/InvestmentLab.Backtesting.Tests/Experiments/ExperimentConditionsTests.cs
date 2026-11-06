using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ExperimentConditionsTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);
    private static readonly ExperimentPeriod Period = new(new DateOnly(2020, 1, 1), new DateOnly(2021, 12, 31));

    [Theory]
    [InlineData(99)]
    [InlineData(1000000001)]
    public void Capital_outside_the_supported_range_is_refused(long capital)
    {
        var conditions = Conditions() with { InitialCapital = capital };

        conditions.Problem(Today)!.Code.ShouldBe("research.capital_invalid");
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(0.11, 0, 0, 0)]
    [InlineData(0, -1, 0, 0)]
    [InlineData(0, 10001, 0, 0)]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 501, 0)]
    [InlineData(0, 0, 0, 501)]
    public void Unsupported_execution_costs_are_refused(double rate, double fee, double spread, double slippage)
    {
        var costs = new TradingCosts((decimal)rate, (decimal)fee, (decimal)spread, (decimal)slippage);
        var conditions = Conditions() with { Costs = costs };

        conditions.Problem(Today)!.Code.ShouldBe("research.costs_invalid");
    }

    [Fact]
    public void Future_or_too_short_periods_are_refused()
    {
        var future = new ExperimentPeriod(Today.AddYears(-1), Today.AddDays(1));
        var shortPeriod = new ExperimentPeriod(Today.AddDays(-20), Today);

        future.Problem(Today)!.Code.ShouldBe("research.period_invalid");
        shortPeriod.Problem(Today)!.Code.ShouldBe("research.period_invalid");
    }

    [Fact]
    public void An_out_of_range_split_is_a_validation_problem_not_a_date_overflow()
    {
        var plan = new RobustnessPlan(OutOfSampleStart: DateOnly.MinValue);

        plan.Problem(Period)!.Code.ShouldBe("research.split_invalid");
    }

    [Fact]
    public void Independent_training_ends_before_the_test_begins()
    {
        var split = new DateOnly(2021, 1, 1);
        var plan = new RobustnessPlan(OutOfSampleStart: split);
        var fold = plan.IndependentTest(Period)!;

        plan.Problem(Period).ShouldBeNull();
        fold.Training!.End.ShouldBe(split.AddDays(-1));
        fold.Test.Start.ShouldBe(split);
    }

    [Fact]
    public void Walk_forward_test_windows_do_not_overlap_and_never_train_on_their_test_dates()
    {
        var windows = new WalkForwardWindows(TrainingMonths: 12, TestMonths: 3);
        var folds = windows.FoldsFor(Period);

        windows.Problem(Period).ShouldBeNull();
        folds.Count.ShouldBe(4);
        folds.ShouldAllBe(fold => fold.Training!.End < fold.Test.Start);
        folds[0].Test.Start.ShouldBe(new DateOnly(2021, 1, 1));
        folds[1].Test.Start.ShouldBe(folds[0].Test.End.AddDays(1));
        folds[^1].Test.End.ShouldBe(Period.End);
    }

    [Fact]
    public void Excessive_or_impossible_walk_forward_plans_are_refused()
    {
        new WalkForwardWindows(3, 1).Problem(Period)!.Code.ShouldBe("research.too_many_folds");
        new WalkForwardWindows(36, 12).Problem(Period)!.Code.ShouldBe("research.walk_forward_invalid");
        new WalkForwardWindows(0, 12).Problem(Period)!.Code.ShouldBe("research.walk_forward_invalid");
    }

    [Fact]
    public void A_parameter_neighborhood_has_a_name_and_a_bounded_number_of_candidates()
    {
        new ParameterNeighborhood("", [1]).Problem()!.Code.ShouldBe("research.parameter_invalid");
        new ParameterNeighborhood("selection.top", [1, 2, 3, 4, 5, 6]).Problem()!.Code.ShouldBe("research.parameter_invalid");
        new ParameterNeighborhood("selection.top", [2, 3, 4]).Problem().ShouldBeNull();
    }

    private static ExperimentConditions Conditions() => new(
        Period: Period,
        InitialCapital: 100_000m,
        Costs: new TradingCosts(),
        Robustness: new RobustnessPlan());
}
