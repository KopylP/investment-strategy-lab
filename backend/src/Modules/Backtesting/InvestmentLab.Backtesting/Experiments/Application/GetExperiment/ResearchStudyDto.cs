using InvestmentLab.Backtesting.Experiments.Application.Shared;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Application.Shared;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperiment;

public sealed record ResearchStudyDto(
    string Kind,
    ResearchFoldDto Window,
    decimal? ParameterValue,
    ReplaySummaryDto? Summary,
    double? TrainingSharpe,
    string? FailureReason)
{
    public static ResearchStudyDto From(ResearchStudy researchStudy) => new(
        Kind: researchStudy.Kind,
        Window: ResearchFoldDto.From(researchStudy.Window),
        ParameterValue: researchStudy.ParameterValue,
        Summary: researchStudy.Summary is { } summary ? ReplaySummaryDto.From(summary) : null,
        TrainingSharpe: researchStudy.TrainingSharpe,
        FailureReason: researchStudy.FailureReason);
}
