using InvestmentLab.Backtesting.Experiments.Application.RunDueExperiments;
using InvestmentLab.Backtesting.Experiments.Application.Shared;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.RequestExperiment;

public sealed record RequestExperimentCommand(Guid StrategyId, int Version, ExperimentConditionsDto Conditions);

public sealed class RequestExperimentHandler(
    IExperimentRepository experiments,
    IResearchStrategies strategies,
    IJobWakeUp wakeUp,
    TimeProvider clock)
{
    public async Task<Result<Guid>> Handle(RequestExperimentCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var conditions = command.Conditions.ToDomain();
        if (conditions.Problem(today) is { } problem)
            return problem;

        var source = await strategies.FindAsync(command.StrategyId, command.Version, cancellationToken);
        if (source is null)
            return BacktestErrors.StrategyNotFound(command.StrategyId);

        if (!source.Setup.CanBeReplayed)
            return ExperimentErrors.NotSupported();

        var candidates = ParameterVariants.Of(source.Parameters, conditions.Robustness.Neighborhood);
        if (candidates.IsFailure)
            return candidates.Error;

        var checkedCandidates = await CheckCandidates(source, candidates.Value, cancellationToken);
        if (checkedCandidates.IsFailure)
            return checkedCandidates.Error;

        var requested = ResearchExperiment.Request(
            owner: source.Setup.OwnerId,
            strategyId: command.StrategyId,
            version: command.Version,
            conditions: conditions,
            now: now);
        if (requested.IsFailure)
            return requested.Error;

        experiments.Add(requested.Value);
        await experiments.SaveAsync(cancellationToken);
        wakeUp.Wake<ExperimentsJob>();

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
