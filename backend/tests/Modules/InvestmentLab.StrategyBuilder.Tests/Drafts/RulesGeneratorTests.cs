using System.Text.Json.Nodes;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class RulesGeneratorTests
{
    private const string MomentumRules =
        """{"entry":{"condition":{"conditions":[{"kind":"compare","left":{"indicator":"blendedMomentumFund"},"operator":"gt","right":{"value":0}},{"kind":"compare","left":{"indicator":"priceVsSma200Fund"},"operator":"gt","right":{"value":0}}],"kind":"all"}},"exit":{"condition":{"direction":"down","indicator":"priceVsSma200Fund","kind":"crossed","level":-0.03,"withinDays":21}},"formatVersion":1,"indicators":{"blendedMomentumFund":{"kind":"blendedMomentum","of":{"series":"fund"}},"priceBenchmark":{"kind":"price","of":{"series":"benchmark"}},"priceVsSma200Fund":{"days":200,"kind":"priceVsSma","of":{"series":"fund"}},"sma200Benchmark":{"days":200,"kind":"sma","of":{"series":"benchmark"}}},"marketFilter":{"condition":{"kind":"compare","left":{"indicator":"priceBenchmark"},"operator":"gt","right":{"indicator":"sma200Benchmark"}},"investWhenUnfavorable":0.3},"selection":{"correlationDays":126,"maxCorrelation":0.7,"maxPerGroup":1,"order":"desc","rankBy":"blendedMomentumFund","top":3},"weights":{"maxPerFund":0.4,"method":"inverseVolatility","targetVolatility":0.14,"targetVolatilityDays":21,"volatilityDays":63}}""";

    private static StrategyDraft Template(string code) => Of(DraftTemplates.GraphOf(code));

    [Fact]
    public void The_momentum_template_generates_the_expected_rules() =>
        RulesGenerator.Generate(Template(DraftTemplates.MomentumTrendFilter)).Json.ShouldBe(MomentumRules);

    [Fact]
    public void The_same_draft_always_gives_the_same_bytes()
    {
        var draft = Template(DraftTemplates.MomentumTrendFilter);
        var reversed = Of(draft.Graph with { Nodes = [.. draft.Graph.Nodes.Reverse()], Links = [.. draft.Graph.Links.Reverse()] });

        RulesGenerator.Generate(reversed).Json.ShouldBe(RulesGenerator.Generate(draft).Json);
    }

    [Fact]
    public void Identical_indicators_merge_into_one_name()
    {
        var graph = Graph(
            [
                Indicator("e-sma", BlockKind.Entry, "sma", days: 200),
                Compare("e-up", BlockKind.Entry, "gt", 0),
                Indicator("x-sma", BlockKind.Exit, "sma", days: 200),
                Compare("x-down", BlockKind.Exit, "lt", 0),
            ],
            [Link("e-sma", "e-up", "left"), Link("e-up", "result-entry", "condition"), Link("x-sma", "x-down", "left"), Link("x-down", "result-exit", "condition")]);

        var rules = RulesGenerator.Generate(Of(graph));

        JsonNode.Parse(rules.Json)!["indicators"]!.AsObject().Select(entry => entry.Key).ShouldBe(["sma200Fund"]);
        rules.SourceMap.NodesOfIndicator("sma200Fund").Select(node => node.Value).ShouldBe(["e-sma", "x-sma"], ignoreOrder: true);
    }

    [Fact]
    public void An_unconnected_exit_leaves_the_exit_out() =>
        JsonNode.Parse(RulesGenerator.Generate(Of(MomentumEntry())).Json)!.AsObject().ContainsKey("exit").ShouldBeFalse();

    [Fact]
    public void Index_hold_has_no_conditions_and_no_ranking()
    {
        var rules = JsonNode.Parse(RulesGenerator.Generate(Template(DraftTemplates.IndexHold)).Json)!.AsObject();

        rules.Select(entry => entry.Key).ShouldBe(["formatVersion", "indicators", "selection", "weights"]);
        rules["selection"]!.AsObject().ContainsKey("rankBy").ShouldBeFalse();
    }

    [Fact]
    public void A_problem_path_finds_its_node()
    {
        var rules = RulesGenerator.Generate(Template(DraftTemplates.MomentumTrendFilter));

        rules.SourceMap.NodeAt("entry.condition.conditions.1.left.indicator")!.Value.ShouldBe("e-trend");
        rules.SourceMap.NodeAt("entry.condition.conditions.1.operator")!.Value.ShouldBe("e-trend-positive");
        rules.SourceMap.NodeAt("marketFilter.investWhenUnfavorable")!.Value.ShouldBe("result-market");
        rules.SourceMap.NodeAt("weights.maxPerFund")!.Value.ShouldBe("result-selection");
        rules.SourceMap.NodeAt("formatVersion").ShouldBeNull();
    }

    [Fact]
    public void Moving_nodes_keeps_the_fingerprint()
    {
        var draft = Template(DraftTemplates.MomentumTrendFilter);
        var before = DraftFingerprint.Of(RulesGenerator.Generate(draft), draft.Setup, draft.Reports);

        draft.MoveNodes(new DraftLayout(new Dictionary<string, NodePosition> { ["e-mom"] = new(10, 20) }, []), Now);

        DraftFingerprint.Of(RulesGenerator.Generate(draft), draft.Setup, draft.Reports).ShouldBe(before);
    }

    [Fact]
    public void Changing_a_fund_changes_the_fingerprint()
    {
        var draft = Template(DraftTemplates.MomentumTrendFilter);
        var rules = RulesGenerator.Generate(draft);

        var other = draft.Setup with { Funds = [FundA] };

        DraftFingerprint.Of(rules, other, draft.Reports).ShouldNotBe(DraftFingerprint.Of(rules, draft.Setup, draft.Reports));
    }
}
