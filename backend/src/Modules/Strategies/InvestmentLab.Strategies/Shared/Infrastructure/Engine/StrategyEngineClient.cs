using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed class StrategyEngineClient(HttpClient http)
{
    public const string DefaultBaseAddress = "https+http://strategy-engine";

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<EngineStrategyType>> GetStrategyTypesAsync(CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<List<EngineStrategyType>>("/v1/strategies", Json, cancellationToken) ?? [];

    public async Task<EngineParametersCheck> CheckParametersAsync(string typeCode, JsonElement parameters, CancellationToken cancellationToken)
    {
        var path = $"/v1/strategies/{Uri.EscapeDataString(typeCode)}/parameters/check";
        using var response = await http.PostAsJsonAsync(path, new { parameters }, Json, cancellationToken);
        response.EnsureSuccessStatusCode();

        var check = await response.Content.ReadFromJsonAsync<EngineParametersCheck>(Json, cancellationToken);

        return check ?? throw new JsonException("The engine answered the parameters check with nothing.");
    }

    public async Task<(JsonNode? Answer, int StatusCode, string? Error)> PostContextAsync(
        string typeCode, string action, EngineContext context, CancellationToken cancellationToken)
    {
        using var response =
            await http.PostAsJsonAsync($"/v1/strategies/{Uri.EscapeDataString(typeCode)}/{action}", context, Json, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return (null, (int)response.StatusCode, DetailOf(body));

        var answer = JsonNode.Parse(body);
        RenameKeys(answer);

        return (answer, (int)response.StatusCode, null);
    }

    public async Task<(string? JobId, int StatusCode, string? Error)> SubmitJobAsync(
        string typeCode, string action, EngineContext context, CancellationToken cancellationToken)
    {
        using var response =
            await http.PostAsJsonAsync($"/v1/strategies/{Uri.EscapeDataString(typeCode)}/{action}/jobs", context, Json, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.IsSuccessStatusCode
            ? (JsonNode.Parse(body)?["jobId"]?.GetValue<string>(), (int)response.StatusCode, null)
            : (null, (int)response.StatusCode, DetailOf(body));
    }

    public async Task<EngineJob?> GetJobAsync(string jobId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync($"/v1/jobs/{Uri.EscapeDataString(jobId)}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var job = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellationToken))!;
        var result = job["result"];
        RenameKeys(result);
        var error = job["error"];

        return new EngineJob(
            job["status"]!.GetValue<string>(),
            result,
            error?["statusCode"]?.GetValue<int>(),
            error?["detail"] is { } detail ? TextOf(detail) : null);
    }

    private static string DetailOf(string body)
    {
        var detail = ParsedOrNull(body) is JsonObject answer ? answer["detail"] : null;

        return detail is null ? body : TextOf(detail);
    }

    private static JsonNode? ParsedOrNull(string body)
    {
        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string TextOf(JsonNode detail) =>
        detail.GetValueKind() == JsonValueKind.String ? detail.GetValue<string>() : detail.ToJsonString();

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
