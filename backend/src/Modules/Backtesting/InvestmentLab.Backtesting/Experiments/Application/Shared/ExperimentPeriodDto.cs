using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ExperimentPeriodDto(
    DateOnly Start,
    DateOnly End)
{
    public static ExperimentPeriodDto From(ExperimentPeriod experimentPeriod) => new(
        Start: experimentPeriod.Start,
        End: experimentPeriod.End);

    public ExperimentPeriod ToDomain() => new(
        Start: Start,
        End: End);
}
