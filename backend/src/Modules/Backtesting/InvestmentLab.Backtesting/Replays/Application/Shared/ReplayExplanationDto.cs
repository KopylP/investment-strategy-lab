using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayExplanationDto(
    string Summary,
    string? EngineVersion,
    IReadOnlyList<ReplayCandidateDto>? Candidates,
    IReadOnlyDictionary<string, string>? ValueLabels)
{
    public static ReplayExplanationDto From(ReplayExplanation replayExplanation) => new(
        Summary: replayExplanation.Summary,
        EngineVersion: replayExplanation.EngineVersion,
        Candidates: replayExplanation.Candidates?.Select(ReplayCandidateDto.From).ToList(),
        ValueLabels: replayExplanation.ValueLabels);
}
