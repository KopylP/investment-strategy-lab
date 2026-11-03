using System.Text.Json;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class EngineParameterChecks(StrategyEngineClient engine) : IParameterChecks
{
    public async Task<Result<ParametersCheck>> CheckAsync(string typeCode, string parameters, CancellationToken cancellationToken)
    {
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

        return new ParametersCheck([.. problems], [.. signalIds]);
    }

    private static string PathOf(IReadOnlyList<JsonElement> segments) =>
        string.Join(".", segments.Select(SegmentText));

    private static string SegmentText(JsonElement segment) =>
        segment.ValueKind == JsonValueKind.Number ? segment.GetInt32().ToString() : segment.GetString() ?? "";
}
