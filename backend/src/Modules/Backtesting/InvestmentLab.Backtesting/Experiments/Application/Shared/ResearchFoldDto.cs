using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchFoldDto(
    ExperimentPeriodDto? Training,
    ExperimentPeriodDto Test)
{
    public static ResearchFoldDto From(ResearchFold researchFold) => new(
        Training: researchFold.Training is { } training ? ExperimentPeriodDto.From(training) : null,
        Test: ExperimentPeriodDto.From(researchFold.Test));
}
