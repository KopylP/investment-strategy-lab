using System.Text.Json;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record NodeParameters(IReadOnlyDictionary<string, JsonElement> Values)
{
    public static readonly NodeParameters Empty = new(new Dictionary<string, JsonElement>());

    public bool Has(string name) => Values.TryGetValue(name, out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

    public double? Number(string name) =>
        Values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    public int? Integer(string name) =>
        Values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;

    public string? Text(string name) =>
        Values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public JsonElement? Element(string name) => Values.TryGetValue(name, out var value) ? value : null;

    public NodeParameters With(string name, JsonElement value)
    {
        var values = new Dictionary<string, JsonElement>(Values) { [name] = value.Clone() };

        return new NodeParameters(values);
    }

    public NodeParameters With(string name, string value) => With(name, JsonSerializer.SerializeToElement(value));

    public NodeParameters With(string name, double value) => With(name, JsonSerializer.SerializeToElement(value));

    public NodeParameters WithObject(string name, object value) => With(name, JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web));
}
