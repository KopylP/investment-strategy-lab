using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public interface ITrialExperiments
{
    Task<Result> CheckAsync(TrialExperiment experiment, string parameters, CancellationToken cancellationToken);

    Task<Result<Guid>> RequestAsync(TrialVersion version, TrialExperiment experiment, CancellationToken cancellationToken);

    Task<TrialSummary> SummaryOfAsync(Guid experimentId, CancellationToken cancellationToken);

    Task<Result> CancelAsync(Guid experimentId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> QueuePositionsAsync(IReadOnlyList<Guid> experimentIds, CancellationToken cancellationToken);
}
