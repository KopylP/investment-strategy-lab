using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ExperimentConditionsDto(
    ExperimentPeriodDto Period,
    decimal InitialCapital,
    ResearchTradingCostsDto Costs,
    RobustnessPlanDto Robustness)
{
    public static ExperimentConditionsDto From(ExperimentConditions experimentConditions) => new(
        Period: ExperimentPeriodDto.From(experimentConditions.Period),
        InitialCapital: experimentConditions.InitialCapital,
        Costs: ResearchTradingCostsDto.From(experimentConditions.Costs),
        Robustness: RobustnessPlanDto.From(experimentConditions.Robustness));

    public ExperimentConditions ToDomain() => new(
        Period: Period.ToDomain(),
        InitialCapital: InitialCapital,
        Costs: Costs.ToDomain(),
        Robustness: Robustness.ToDomain());
}
