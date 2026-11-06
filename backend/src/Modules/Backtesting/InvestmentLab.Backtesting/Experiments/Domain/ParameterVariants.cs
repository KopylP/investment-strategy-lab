using System.Text.Json.Nodes;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public static class ParameterVariants
{
    private const int MaximumPathSegments = 24;

    public static Result<IReadOnlyList<ParameterVariant>> Of(string parameters, ParameterNeighborhood? neighborhood)
    {
        if (neighborhood is null)
            return Result<IReadOnlyList<ParameterVariant>>.Success([new ParameterVariant(parameters, null)]);

        var path = neighborhood.Path.Split('.');
        if (path.Length > MaximumPathSegments)
            return ExperimentErrors.InvalidParameter();

        var document = JsonNode.Parse(parameters)!;
        var number = NumberAt(document, path);
        if (number is null)
            return ExperimentErrors.InvalidParameter();

        var values = neighborhood.Values.Prepend(number.Value).Distinct();
        var variants = values.Select(value => Replacing(document, path, value)).ToList();

        return variants;
    }

    private static decimal? NumberAt(JsonNode document, IReadOnlyList<string> path)
    {
        var node = At(document, path);
        if (node is not JsonValue value)
            return null;

        return value.TryGetValue<decimal>(out var number) ? number : null;
    }

    private static ParameterVariant Replacing(JsonNode document, string[] path, decimal value)
    {
        var changed = document.DeepClone();
        var parent = At(changed, path[..^1])!;
        var property = path[^1];

        if (parent is JsonObject properties)
            properties[property] = value;
        else if (parent is JsonArray elements)
            elements[int.Parse(property)] = value;

        return new ParameterVariant(changed.ToJsonString(), value);
    }

    private static JsonNode? At(JsonNode document, IReadOnlyList<string> path)
    {
        var current = document;

        foreach (var part in path)
        {
            current = Child(current, part);
            if (current is null)
                return null;
        }

        return current;
    }

    private static JsonNode? Child(JsonNode? parent, string part) => parent switch
    {
        JsonObject properties => properties[part],
        JsonArray elements when IsArrayIndex(part, elements.Count, out var index) => elements[index],
        _ => null,
    };

    private static bool IsArrayIndex(string part, int count, out int index) =>
        int.TryParse(part, out index) && index >= 0 && index < count;
}
