using System.Text.Json;
using System.Text.Json.Serialization;
using InvestmentLab.BuildingBlocks.Localization;

namespace InvestmentLab.BuildingBlocks.Mcp;

public sealed class LocalizedTextConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(MessageLocalizer.Default.Render(value));
}
