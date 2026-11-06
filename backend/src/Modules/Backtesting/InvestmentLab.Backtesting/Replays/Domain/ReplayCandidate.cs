using System.Text.Json;

namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayCandidate(
    Guid InstrumentId,
    string Name,
    string Status,
    string Reason,
    decimal? Weight,
    IReadOnlyDictionary<string, JsonElement>? Values = null);
