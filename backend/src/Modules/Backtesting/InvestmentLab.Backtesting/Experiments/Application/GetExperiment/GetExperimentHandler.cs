using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperiment;

public sealed record ExperimentDto(
    Guid Id,
    Guid StrategyId,
    int StrategyVersion,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? FinishedAt,
    string? FailureReason,
    ExperimentConditions Conditions,
    ResearchReport? Result);

public sealed class GetExperimentHandler(IExperimentRepository experiments)
{
    public async Task<Result<ExperimentDto>> Handle(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(id, cancellationToken);
        if (experiment is null)
            return ExperimentErrors.NotFound(id);

        var report = WithoutJournal(experiment.Result);

        return new ExperimentDto(
            Id: experiment.Id,
            StrategyId: experiment.StrategyId,
            StrategyVersion: experiment.StrategyVersion,
            Status: experiment.Status.ToString(),
            RequestedAt: experiment.RequestedAt,
            FinishedAt: experiment.FinishedAt,
            FailureReason: experiment.FailureReason,
            Conditions: experiment.Conditions,
            Result: report);
    }

    private static ResearchReport? WithoutJournal(ResearchReport? report)
    {
        if (report is null)
            return null;

        var baseline = report.Baseline with { Journal = null };

        return report with { Baseline = baseline };
    }
}
