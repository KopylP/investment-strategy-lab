using System.Text.Json;
using System.Text.Json.Nodes;
using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Mcp;

public static class McpReply
{
    private const int FractionDigits = 4;

    public static Language Language { get; } = Language.English;

    public static string Rendered(string reply, MessageLocalizer localizer)
    {
        if (ParsedOrNull(reply) is not { } json)
            return localizer.Render(reply);

        var rendered = RenderedNode(json, localizer);

        return rendered?.ToJsonString(McpJson.Options) ?? "null";
    }

    private static JsonNode? ParsedOrNull(string reply)
    {
        try
        {
            return JsonNode.Parse(reply);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonNode? RenderedNode(JsonNode? node, MessageLocalizer localizer) => node switch
    {
        JsonObject json => RenderedObject(json, localizer),
        JsonArray array => new JsonArray([.. array.Select(item => RenderedNode(item, localizer))]),
        JsonValue value => RenderedValue(value, localizer),
        _ => null,
    };

    private static JsonObject RenderedObject(JsonObject json, MessageLocalizer localizer)
    {
        var rendered = new JsonObject();
        foreach (var (name, value) in json)
        {
            if (value is not null)
                rendered[name] = RenderedNode(value, localizer);
        }

        return rendered;
    }

    private static JsonValue RenderedValue(JsonValue value, MessageLocalizer localizer) => value.GetValueKind() switch
    {
        JsonValueKind.String => JsonValue.Create(localizer.Render(value.GetValue<string>())),
        JsonValueKind.Number => RoundedNumber(value),
        _ => (JsonValue)value.DeepClone(),
    };

    private static JsonValue RoundedNumber(JsonValue value)
    {
        if (value.TryGetValue<long>(out var whole))
            return JsonValue.Create(whole);

        var rounded = Math.Round(value.GetValue<double>(), FractionDigits);

        return JsonValue.Create(rounded);
    }
}
