using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.RequestExperiment;

public sealed record RequestExperimentCommand(Guid StrategyId, int Version, ExperimentConditions Conditions);

public sealed class RequestExperimentHandler(
    IExperimentRepository experiments,
    IResearchStrategies strategies,
    TimeProvider clock)
{
    public async Task<Result<Guid>> Handle(RequestExperimentCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (command.Conditions.Problem(today) is { } problem)
            return problem;

        var source = await strategies.FindAsync(command.StrategyId, command.Version, cancellationToken);
        if (source is null)
            return BacktestErrors.StrategyNotFound(command.StrategyId);

        if (!source.Setup.CanBeReplayed)
            return ExperimentErrors.NotSupported();

        var candidates = ParameterVariants.Of(source.Parameters, command.Conditions.Robustness.Neighborhood);
        if (candidates.IsFailure)
            return candidates.Error;

        var checkedCandidates = await CheckCandidates(source, candidates.Value, cancellationToken);
        if (checkedCandidates.IsFailure)
            return checkedCandidates.Error;

        var requested = ResearchExperiment.Request(
            owner: source.Setup.OwnerId,
            strategyId: command.StrategyId,
            version: command.Version,
            conditions: command.Conditions,
            now: now);
        if (requested.IsFailure)
            return requested.Error;

        experiments.Add(requested.Value);
        await experiments.SaveAsync(cancellationToken);

        return requested.Value.Id;
    }

    private async Task<Result> CheckCandidates(
        ResearchSource source,
        IReadOnlyList<ParameterVariant> candidates,
        CancellationToken cancellationToken)
    {
        foreach (var candidate in candidates)
        {
            var check = await strategies.CheckAsync(source.TypeCode, candidate.Parameters, cancellationToken);
            if (check.IsFailure)
                return Result.Failure(check.Error);
        }

        return Result.Success();
    }
}
