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
    public void The_market_filter_mode_survives_import_and_generation()
    {
        const string market = ""","indicators":{"spy":{"kind":"price","of":{"series":"benchmark"}}},"marketFilter":{"condition":{"kind":"compare","left":{"indicator":"spy"},"operator":"gt","right":{"value":1}},"investWhenUnfavorable":0.2,"whenUnfavorable":"newMoneyOnly"}""";
        var json = """{"formatVersion":1""" + market + ""","selection":{"top":3},"weights":{"method":"equal"}}""";

        var imported = Import(json).Value;

        var generated = RulesGenerator.Generate(Of(imported.Graph)).Json;
        generated.ShouldContain("\"whenUnfavorable\":\"newMoneyOnly\"");
    }

    [Fact]
    public void The_template_with_a_market_filter_reduces_holdings()
    {
        var generated = RulesGenerator.Generate(Of(DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter))).Json;

        generated.ShouldContain("\"whenUnfavorable\":\"reduceHoldings\"");
    }

    [Fact]
    public void Import_reuses_indicators_shared_by_multiple_results()
    {
        var source = MomentumEntry();
        source = source with { Links = [.. source.Links, Link("mom", "result-selection", "rankBy"), Link("positive", "result-exit", "condition")] };
        var json = RulesGenerator.Generate(Of(source)).Json;
        var imported = Import(json).Value.Graph;
        imported.Nodes.Count(node => node.Kind == NodeKind.Indicator).ShouldBe(1);
        imported.Nodes.Where(node => !NodeKinds.IsResult(node.Kind)).ShouldAllBe(node => node.Block == BlockKind.Shared);
        RulesGenerator.Generate(Of(imported)).Json.ShouldBe(json);
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
