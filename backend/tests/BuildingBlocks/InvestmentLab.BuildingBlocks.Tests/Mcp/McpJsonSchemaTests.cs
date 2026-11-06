using System.ComponentModel;
using System.Text.Json;
using InvestmentLab.BuildingBlocks.Mcp;
using ModelContextProtocol.Server;

namespace InvestmentLab.BuildingBlocks.Tests.Mcp;

public sealed class McpJsonSchemaTests
{
    public enum JournalKind
    {
        Rebalance,
        Exit,
    }

    [Description("A tool with the parameter shapes the real tools use.")]
    private static string Rename(Guid draftId, string? name, string label, JournalKind? kind) => $"{draftId}{name}{label}{kind}";

    private static JsonElement PropertiesOfRename()
    {
        var tool = McpServerTool.Create(Rename, new McpServerToolCreateOptions { Name = "rename", SerializerOptions = McpJson.Options });

        return tool.ProtocolTool.InputSchema.GetProperty("properties");
    }

    [Fact]
    public void A_nullable_string_parameter_is_described_as_a_string_or_null()
    {
        var type = PropertiesOfRename().GetProperty("name").GetProperty("type");

        type.EnumerateArray().Select(item => item.GetString()).ShouldBe(["string", "null"], ignoreOrder: true);
    }

    [Fact]
    public void A_string_parameter_is_described_as_a_string()
    {
        PropertiesOfRename().GetProperty("label").GetProperty("type").GetString().ShouldBe("string");
    }

    [Fact]
    public void An_enum_parameter_lists_its_names()
    {
        var kind = PropertiesOfRename().GetProperty("kind");

        kind.GetProperty("enum").EnumerateArray().Select(item => item.ValueKind == JsonValueKind.Null ? null : item.GetString())
            .ShouldContain("Rebalance");
    }
}
