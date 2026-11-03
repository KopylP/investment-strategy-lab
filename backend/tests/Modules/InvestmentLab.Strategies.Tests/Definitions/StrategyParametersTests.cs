using System.Text.Json;
using System.Text.Json.Nodes;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Application.Shared;

namespace InvestmentLab.Strategies.Tests.Definitions;

public sealed class StrategyParametersTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    private static StrategyTypeDescriptor TypeWithDefaults(string? defaults) =>
        StrategyTypeDescriptor.Of("rule-based", "Rule-based", "", "python", "1", false, ["instrument"], """{"type":"object"}""", defaults, [], Now);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Parameters_of_a_type_without_defaults_are_taken_as_given()
    {
        var result = StrategySetupFactory.Parameters(TypeWithDefaults(null), Json("""{"a":{"b":1}}"""));

        JsonNode.DeepEquals(JsonNode.Parse(result.Value), JsonNode.Parse("""{"a":{"b":1}}""")).ShouldBeTrue();
    }

    [Fact]
    public void Parameters_of_a_type_with_defaults_are_merged_into_them()
    {
        var result = StrategySetupFactory.Parameters(TypeWithDefaults("""{"a":{"b":1,"c":2}}"""), Json("""{"a":{"b":5}}"""));

        JsonNode.DeepEquals(JsonNode.Parse(result.Value), JsonNode.Parse("""{"a":{"b":5,"c":2}}""")).ShouldBeTrue();
    }

    [Fact]
    public void A_type_without_defaults_needs_an_object()
    {
        var result = StrategySetupFactory.Parameters(TypeWithDefaults(null), null);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("strategy.parameters_invalid");
    }
}
