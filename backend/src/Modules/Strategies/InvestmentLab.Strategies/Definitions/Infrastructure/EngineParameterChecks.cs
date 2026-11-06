using System.Text.Json;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.Extensions.Caching.Memory;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class EngineParameterChecks(StrategyEngineClient engine, IMemoryCache? cache = null) : IParameterChecks
{
    public async Task<Result<ParametersCheck>> CheckAsync(string typeCode, string parameters, CancellationToken cancellationToken)
    {
        var key = (typeof(EngineParameterChecks), typeCode, parameters);
        if (cache?.TryGetValue(key, out ParametersCheck? cached) == true && cached is not null)
            return cached;
        EngineParametersCheck answer;
        try
        {
            answer = await engine.CheckParametersAsync(typeCode, JsonDocument.Parse(parameters).RootElement, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException or JsonException)
        {
            return StrategyErrors.EngineUnavailable(exception.Message);
        }

        var problems = answer.Problems.Select(problem => new ParameterProblem(PathOf(problem.Path), problem.Code, problem.Message));
        var signalIds = answer.SignalIds.Select(Guid.Parse);

        var check = new ParametersCheck([.. problems], [.. signalIds], Math.Max(1, answer.HistoryBars));
        if (check.IsClean)
            cache?.Set(key, check, TimeSpan.FromMinutes(10));
        return check;
    }

    private static string PathOf(IReadOnlyList<JsonElement> segments) =>
        string.Join(".", segments.Select(SegmentText));

    private static string SegmentText(JsonElement segment) =>
        segment.ValueKind == JsonValueKind.Number ? segment.GetInt32().ToString() : segment.GetString() ?? "";
}
