using System.Text.Json.Nodes;
using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.BuildingBlocks.Mcp;

namespace InvestmentLab.BuildingBlocks.Tests.Mcp;

public sealed class McpReplyTests
{
    private static readonly MessageLocalizer Localizer = new(InvestmentLab.SharedKernel.Language.Ukrainian, new Dictionary<string, string>
    {
        ["test.market"] = "Якщо {condition}, у фонди йде все",
        ["test.compare"] = "{left} вище за {right}",
    });

    [Fact]
    public void Nested_message_codes_are_rendered_in_ukrainian()
    {
        const string reply = """{"words":[{"sentence":"@test.market {\"condition\":\"@test.compare {\\\"left\\\":\\\"SPY\\\",\\\"right\\\":\\\"SMA\\\"}\"}"}]}""";

        var rendered = McpReply.Rendered(reply, Localizer);

        JsonNode.Parse(rendered)!["words"]![0]!["sentence"]!.GetValue<string>().ShouldBe("Якщо SPY вище за SMA, у фонди йде все");
    }

    [Fact]
    public void Fractions_are_rounded_and_empty_fields_dropped()
    {
        const string reply = """{"sharpe":0.7933006433540044,"failureReason":null,"trades":258,"rows":[{"weight":0.123456789,"note":null}]}""";

        McpReply.Rendered(reply, Localizer).ShouldBe("""{"sharpe":0.7933,"trades":258,"rows":[{"weight":0.1235}]}""");
    }

    [Fact]
    public void A_reply_that_is_no_json_is_rendered_as_one_message()
    {
        McpReply.Rendered("@test.compare {\"left\":\"A\",\"right\":\"B\"}", Localizer).ShouldBe("A вище за B");
        McpReply.Rendered("Done.", Localizer).ShouldBe("Done.");
    }
}
