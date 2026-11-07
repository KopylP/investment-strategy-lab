using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ExperimentCancellationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_queued_experiment_can_be_cancelled()
    {
        var experiment = Queued();

        var cancelled = experiment.Cancel(Now);

        cancelled.IsSuccess.ShouldBeTrue();
        experiment.Status.ShouldBe(ExperimentStatus.Cancelled);
        experiment.FinishedAt.ShouldBe(Now);
    }

    [Fact]
    public void A_running_experiment_cannot_be_cancelled()
    {
        var experiment = Queued();
        experiment.Start();

        var cancelled = experiment.Cancel(Now);

        cancelled.Error.Code.ShouldBe("research.not_queued");
        experiment.Status.ShouldBe(ExperimentStatus.Running);
    }

    [Fact]
    public void A_cancelled_experiment_does_not_start()
    {
        var experiment = Queued();
        experiment.Cancel(Now);

        var started = experiment.Start();

        started.IsFailure.ShouldBeTrue();
        experiment.Status.ShouldBe(ExperimentStatus.Cancelled);
    }

    [Fact]
    public void An_interrupted_running_experiment_starts_again()
    {
        var experiment = Queued();
        experiment.Start();

        var restarted = experiment.Start();

        restarted.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_failure_does_not_overwrite_a_cancellation()
    {
        var experiment = Queued();
        experiment.Cancel(Now);

        experiment.Fail("research.failed", Now.AddMinutes(1));

        experiment.Status.ShouldBe(ExperimentStatus.Cancelled);
        experiment.FailureReason.ShouldBeNull();
    }

    private static ResearchExperiment Queued()
    {
        var conditions = new ExperimentConditions(
            Period: new ExperimentPeriod(new DateOnly(2020, 1, 1), new DateOnly(2021, 12, 31)),
            InitialCapital: 100_000m,
            Costs: new TradingCosts(),
            Robustness: new RobustnessPlan());

        return ResearchExperiment.Request(UserId.New(), Guid.NewGuid(), 1, conditions, Now).Value;
    }
}
