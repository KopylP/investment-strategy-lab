using InvestmentLab.Backtesting.Experiments.Application.Shared;
using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperiment;

public sealed record ResearchStudyDto(
    string Kind,
    ResearchFoldDto Window,
    decimal? ParameterValue,
    ResearchSummaryDto? Summary,
    double? TrainingSharpe,
    string? FailureReason)
{
    public static ResearchStudyDto From(ResearchStudy researchStudy) => new(
        Kind: researchStudy.Kind,
        Window: ResearchFoldDto.From(researchStudy.Window),
        ParameterValue: researchStudy.ParameterValue,
        Summary: researchStudy.Summary is { } summary ? ResearchSummaryDto.From(summary) : null,
        TrainingSharpe: researchStudy.TrainingSharpe,
        FailureReason: researchStudy.FailureReason);
}
