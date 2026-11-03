using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class CanonicalJson
{
    public static string Write(JsonNode? node)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            WriteNode(writer, node);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteNode(Utf8JsonWriter writer, JsonNode? node)
    {
        switch (node)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonObject obj:
                WriteObject(writer, obj);
                break;
            case JsonArray array:
                WriteArray(writer, array);
                break;
            default:
                WriteValue(writer, JsonSerializer.SerializeToElement(node));
                break;
        }
    }

    private static void WriteObject(Utf8JsonWriter writer, JsonObject obj)
    {
        writer.WriteStartObject();
        foreach (var (name, value) in obj.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            writer.WritePropertyName(name);
            WriteNode(writer, value);
        }
        writer.WriteEndObject();
    }

    private static void WriteArray(Utf8JsonWriter writer, JsonArray array)
    {
        writer.WriteStartArray();
        foreach (var item in array)
            WriteNode(writer, item);
        writer.WriteEndArray();
    }

    private static void WriteValue(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                writer.WriteNumberValue(value.GetDouble());
                break;
            case JsonValueKind.Object or JsonValueKind.Array:
                WriteNode(writer, JsonNode.Parse(value.GetRawText()));
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }
}
