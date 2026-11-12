using InvestmentLab.Backtesting.Experiments.Application.Shared;
using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.CheckExperiment;

public sealed record CheckExperimentCommand(ExperimentConditionsDto Conditions, string TypeCode, string Parameters);

public sealed class CheckExperimentHandler(IResearchStrategies strategies, TimeProvider clock)
{
    public async Task<Result> Handle(CheckExperimentCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var conditions = command.Conditions.ToDomain();
        if (conditions.Problem(today) is { } problem)
            return Result.Failure(problem);

        var neighborhood = conditions.Robustness.Neighborhood;
        var candidates = ParameterVariants.Of(command.Parameters, neighborhood);
        if (candidates.IsFailure)
            return Result.Failure(candidates.Error);

        foreach (var candidate in candidates.Value)
        {
            var check = await strategies.CheckAsync(command.TypeCode, candidate.Parameters, cancellationToken);
            if (check.IsFailure)
                return Result.Failure(check.Error);
        }

        return Result.Success();
    }
}
