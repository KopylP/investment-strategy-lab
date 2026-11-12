using InvestmentLab.Backtesting.Replays.Domain;
using System.Text.Json;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayCandidateDto(
    Guid InstrumentId,
    string Name,
    string Status,
    string Reason,
    decimal? Weight,
    IReadOnlyDictionary<string, JsonElement>? Values)
{
    public static ReplayCandidateDto From(ReplayCandidate replayCandidate) => new(
        InstrumentId: replayCandidate.InstrumentId,
        Name: replayCandidate.Name,
        Status: replayCandidate.Status,
        Reason: replayCandidate.Reason,
        Weight: replayCandidate.Weight,
        Values: replayCandidate.Values);
}
