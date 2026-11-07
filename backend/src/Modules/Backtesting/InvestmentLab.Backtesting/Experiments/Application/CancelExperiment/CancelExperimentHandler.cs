using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.CancelExperiment;

public sealed class CancelExperimentHandler(IExperimentRepository experiments, TimeProvider clock)
{
    public async Task<Result> Handle(Guid experimentId, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(experimentId, cancellationToken);
        if (experiment is null)
            return ExperimentErrors.NotFound(experimentId);

        var cancelled = experiment.Cancel(clock.GetUtcNow());
        if (cancelled.IsFailure)
            return cancelled;

        var saved = await experiments.TrySaveAsync(cancellationToken);

        return saved ? Result.Success() : ExperimentErrors.NotQueued();
    }
}
