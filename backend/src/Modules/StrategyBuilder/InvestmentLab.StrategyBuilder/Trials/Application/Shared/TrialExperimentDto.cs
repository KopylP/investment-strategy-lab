using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed record TrialExperimentDto(
    TrialPeriodDto Period,
    decimal InitialCapital,
    TrialExecutionCostsDto Costs,
    TrialRobustnessDto Robustness)
{
    public static TrialExperimentDto From(TrialExperiment experiment) => new(
        Period: TrialPeriodDto.From(experiment.Period),
        InitialCapital: experiment.InitialCapital,
        Costs: TrialExecutionCostsDto.From(experiment.Costs),
        Robustness: TrialRobustnessDto.From(experiment.Robustness));

    public Result<TrialExperiment> ToDomain()
    {
        if (Period is null || Costs is null || Robustness is null)
            return TrialErrors.ExperimentConditionsMissing();

        var robustness = Robustness.ToDomain();
        if (robustness.IsFailure)
            return robustness.Error;

        return new TrialExperiment(
            Period: Period.ToDomain(),
            InitialCapital: InitialCapital,
            Costs: Costs.ToDomain(),
            Robustness: robustness.Value);
    }
}
