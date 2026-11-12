using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Application.Shared;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ExperimentConditionsDto(
    ExperimentPeriodDto Period,
    decimal InitialCapital,
    TradingCostsDto Costs,
    RobustnessPlanDto Robustness)
{
    public static ExperimentConditionsDto From(ExperimentConditions experimentConditions) => new(
        Period: ExperimentPeriodDto.From(experimentConditions.Period),
        InitialCapital: experimentConditions.InitialCapital,
        Costs: TradingCostsDto.From(experimentConditions.Costs),
        Robustness: RobustnessPlanDto.From(experimentConditions.Robustness));

    public ExperimentConditions ToDomain() => new(
        Period: Period.ToDomain(),
        InitialCapital: InitialCapital,
        Costs: Costs.ToDomain(),
        Robustness: Robustness.ToDomain());
}
