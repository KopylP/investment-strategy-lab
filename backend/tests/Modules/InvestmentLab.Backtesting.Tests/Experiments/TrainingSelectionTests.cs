using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class TrainingSelectionTests
{
    [Fact]
    public void Selection_uses_the_highest_training_sharpe()
    {
        var baseline = Measured(3m, .4);
        var neighbor = Measured(4m, .9);

        TrainingSelection.HighestSharpe([baseline, neighbor]).ShouldBe(neighbor);
    }

    [Fact]
    public void Equal_scores_keep_the_baseline_instead_of_changing_parameters()
    {
        var baseline = Measured(3m, .9);
        var neighbor = Measured(4m, .9);

        TrainingSelection.HighestSharpe([baseline, neighbor]).ShouldBe(baseline);
    }

    [Fact]
    public void Non_finite_scores_and_missing_training_results_do_not_create_a_winner()
    {
        TrainingSelection.HighestSharpe([Measured(3m, double.NaN), Measured(4m, double.PositiveInfinity)]).ShouldBeNull();
        TrainingSelection.HighestSharpe([]).ShouldBeNull();
    }

    private static TrainingResult Measured(decimal value, double sharpe)
    {
        var candidate = new ParameterVariant("{}", value);
        var summary = new ReplaySummary(.1, .05, .1, sharpe, -.1);

        return new TrainingResult(candidate, summary);
    }
}
