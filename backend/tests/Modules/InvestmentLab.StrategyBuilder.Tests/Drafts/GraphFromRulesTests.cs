using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class GraphFromRulesTests
{
    public static TheoryData<string> Templates => [.. DraftTemplates.Codes];

    private const string Minimal = """{"formatVersion":1,"indicators":{},"selection":{"top":3},"weights":{"method":"equal"}""";

    private static Result<ImportedRules> Import(string json) => GraphFromRules.Build(json, EngineSchema.Vocabulary);

    [Theory]
    [MemberData(nameof(Templates))]
    public void Generating_reading_and_generating_again_gives_the_same_json(string template)
    {
        var generated = RulesGenerator.Generate(Of(DraftTemplates.GraphOf(template))).Json;

        var imported = Import(generated).Value;

        RulesGenerator.Generate(Of(imported.Graph)).Json.ShouldBe(generated);
    }

    [Fact]
    public void Unknown_fields_refuse_the_import()
    {
        var error = Import(Minimal + ""","entry":{"condition":{"kind":"signal","signalId":"x","state":"active","scope":"anyInstrument"},"foo":1}}""").Error;

        error.Code.ShouldBe("builder.import.field_unknown");
        error.AsMessage().ShouldContain("entry.foo");
    }

    [Fact]
    public void An_unknown_indicator_kind_refuses_the_import() =>
        Import("""{"formatVersion":1,"indicators":{"x":{"kind":"macd","of":{"series":"fund"}}},"selection":{"top":3},"weights":{"method":"equal"}}""")
            .Error.Code.ShouldBe("builder.import.indicator_unknown");

    [Fact]
    public void A_reference_to_a_missing_indicator_refuses_the_import() =>
        Import(Minimal + ""","entry":{"condition":{"kind":"compare","left":{"indicator":"nope"},"operator":"gt","right":{"value":0}}}}""")
            .Error.Code.ShouldBe("builder.import.reference_unknown");

    [Fact]
    public void Only_format_version_1_is_read() =>
        Import("""{"formatVersion":2}""").Error.Code.ShouldBe("builder.import.format_version");

    [Fact]
    public void Text_that_is_no_json_is_refused() => Import("rules").Error.Code.ShouldBe("builder.import.not_json");

    [Fact]
    public void Imported_nodes_have_no_positions()
    {
        var generated = RulesGenerator.Generate(Of(DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter))).Json;

        var draft = Of(Import(generated).Value.Graph);

        draft.Layout.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void Extra_series_of_the_rules_are_collected()
    {
        const string Rules = """
            {"formatVersion":1,"indicators":{"gold":{"kind":"price","of":{"series":"extra","instrumentId":"GOLD"}}},
             "marketFilter":{"condition":{"kind":"compare","left":{"indicator":"gold"},"operator":"gt","right":{"value":1}},"investWhenUnfavorable":0.5},
             "selection":{"top":3},"weights":{"method":"equal"}}
            """;

        Import(Rules.Replace("GOLD", Gold.ToString())).Value.ExtraSeries.ShouldBe([Gold]);
    }
}
