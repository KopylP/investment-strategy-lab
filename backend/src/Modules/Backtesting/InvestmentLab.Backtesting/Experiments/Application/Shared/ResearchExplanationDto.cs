using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchExplanationDto(
    string Summary,
    string? EngineVersion,
    IReadOnlyList<ResearchCandidateDto>? Candidates,
    IReadOnlyDictionary<string, string>? ValueLabels)
{
    public static ResearchExplanationDto From(ReplayExplanation replayExplanation) => new(
        Summary: replayExplanation.Summary,
        EngineVersion: replayExplanation.EngineVersion,
        Candidates: replayExplanation.Candidates?.Select(ResearchCandidateDto.From).ToList(),
        ValueLabels: replayExplanation.ValueLabels);
}
