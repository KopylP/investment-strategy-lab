using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ExperimentQueueTests
{
    [Fact]
    public void The_running_experiment_is_at_zero_and_queued_ones_count_from_one_in_request_order()
    {
        var running = new UnfinishedExperiment(Guid.NewGuid(), ExperimentStatus.Running);
        var first = new UnfinishedExperiment(Guid.NewGuid(), ExperimentStatus.Queued);
        var second = new UnfinishedExperiment(Guid.NewGuid(), ExperimentStatus.Queued);

        var positions = ExperimentQueue.PositionsOf([first, running, second]);

        positions[running.Id].ShouldBe(0);
        positions[first.Id].ShouldBe(1);
        positions[second.Id].ShouldBe(2);
    }
}
