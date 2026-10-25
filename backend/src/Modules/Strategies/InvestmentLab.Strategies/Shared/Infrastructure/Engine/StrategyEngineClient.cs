using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

/// <summary>
/// HTTP client of the Python strategy engine (contract v1, camelCase JSON). Engine payloads name instruments
/// by <c>key</c>; the backend sends instrument ids as keys, so answers are read back with <c>instrumentId</c>.
/// </summary>
public sealed class StrategyEngineClient(HttpClient http)
{
    public const string DefaultBaseAddress = "https+http://strategy-engine";

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<EngineStrategyType>> GetStrategyTypesAsync(CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<List<EngineStrategyType>>("/v1/strategies", Json, cancellationToken) ?? [];

    /// <summary>Posts an evaluation context; returns the answer with keys renamed, or the engine's error text.</summary>
    public async Task<(JsonNode? Answer, int StatusCode, string? Error)> PostContextAsync(
        string typeCode, string action, EngineContext context, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync($"/v1/strategies/{Uri.EscapeDataString(typeCode)}/{action}", context, Json, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            return (null, (int)response.StatusCode, body);

        var answer = JsonNode.Parse(body);
        RenameKeys(answer);
        return (answer, (int)response.StatusCode, null);
    }

    private static void RenameKeys(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["key"] is { } key)
                {
                    obj.Remove("key");
                    obj["instrumentId"] = key;
                }

                foreach (var (_, child) in obj.ToList())
                    RenameKeys(child);
                break;
            case JsonArray array:
                foreach (var item in array)
                    RenameKeys(item);
                break;
        }
    }
}

public sealed record EngineStrategyType(
    string Code,
    string Name,
    string Description,
    string Engine,
    string Version,
    bool SupportsState,
    IReadOnlyList<string> RequiredSeriesRoles,
    JsonElement ParametersSchema,
    JsonElement DefaultParameters);

public sealed record EngineContext(
    DateOnly AsOf,
    JsonElement Parameters,
    IReadOnlyList<EngineUniverseMember> Universe,
    string? CashInstrumentKey,
    IReadOnlyList<EngineSeries> MarketSeries,
    EngineSubPortfolio SubPortfolio);

public sealed record EngineUniverseMember(string Key, string? Name, string? Group);

/// <param name="Role">instrument, benchmark or volatilityIndex.</param>
public sealed record EngineSeries(string Key, string Role, IReadOnlyList<DateOnly> Dates, IReadOnlyList<decimal?> AdjustedClose, IReadOnlyList<decimal?> Close);

public sealed record EngineSubPortfolio(string Currency, decimal Cash, decimal Contribution, IReadOnlyList<EngineHolding> Holdings);

public sealed record EngineHolding(string Key, IReadOnlyList<EngineLot> Lots);

public sealed record EngineLot(decimal Quantity, decimal Price, DateOnly? AcquiredOn);
