using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class GraphCheckTests
{
    private static IReadOnlyList<DraftProblem> Problems(StrategyGraph graph, DraftSetup? setup = null) =>
        GraphCheck.Of(Of(graph, setup), EngineSchema.Vocabulary);

    private static IReadOnlyList<string> Codes(StrategyGraph graph, DraftSetup? setup = null) =>
        [.. Problems(graph, setup).Select(problem => problem.Code)];

    [Fact]
    public void A_simple_draft_has_no_problems() => Problems(MomentumEntry()).ShouldBeEmpty();

    [Fact]
    public void A_cycle_is_a_problem()
    {
        var graph = Graph(
            [Logic("and", BlockKind.Entry, NodeKind.All), Logic("no", BlockKind.Entry, NodeKind.Not)],
            [Link("and", "no", "condition"), Link("no", "and", "conditions"), Link("and", "result-entry", "condition")]);

        Codes(graph).ShouldContain("builder.problem.cycle");
    }

    [Fact]
    public void A_link_across_blocks_is_a_problem()
    {
        var graph = Graph(
            [Indicator("mom", BlockKind.Exit, "blendedMomentum"), Compare("positive", BlockKind.Entry, "gt", 0)],
            [Link("mom", "positive", "left"), Link("positive", "result-entry", "condition")]);

        Problems(graph).ShouldContain(problem => problem.Code == "builder.problem.link_across_blocks" && problem.Node == new NodeId("positive"));
    }

    [Fact]
    public void A_number_cannot_feed_a_condition_input()
    {
        var graph = Graph(
            [Indicator("mom", BlockKind.Entry, "blendedMomentum"), Logic("and", BlockKind.Entry, NodeKind.All)],
            [Link("mom", "and", "conditions"), Link("and", "result-entry", "condition")]);

        Codes(graph).ShouldContain("builder.problem.socket_mismatch");
    }

    [Fact]
    public void A_compare_needs_its_left_side()
    {
        var graph = Graph([Compare("positive", BlockKind.Entry, "gt", 0)], [Link("positive", "result-entry", "condition")]);

        Problems(graph).ShouldContain(problem => problem.Code == "builder.problem.input_missing" && problem.Node == new NodeId("positive"));
    }

    [Fact]
    public void A_compare_needs_a_right_side_or_a_number()
    {
        var graph = Graph(
            [Indicator("mom", BlockKind.Entry, "blendedMomentum"), Compare("positive", BlockKind.Entry, "gt")],
            [Link("mom", "positive", "left"), Link("positive", "result-entry", "condition")]);

        Codes(graph).ShouldContain("builder.problem.input_missing");
    }

    [Fact]
    public void A_node_nobody_reads_is_a_warning()
    {
        var graph = MomentumEntry() with { Nodes = [.. MomentumEntry().Nodes, Indicator("lonely", BlockKind.Entry, "price")] };

        var problem = Problems(graph).ShouldHaveSingleItem();

        problem.Code.ShouldBe("builder.problem.dangling_node");
        problem.Severity.ShouldBe(ProblemSeverity.Warning);
    }

    [Fact]
    public void An_indicator_the_vocabulary_does_not_know_is_a_problem()
    {
        var graph = Graph(
            [Indicator("mom", BlockKind.Entry, "macd"), Compare("positive", BlockKind.Entry, "gt", 0)],
            [Link("mom", "positive", "left"), Link("positive", "result-entry", "condition")]);

        Problems(graph).ShouldContain(problem => problem.Code == "builder.problem.indicator_unknown" && problem.Node == new NodeId("mom"));
    }

    [Fact]
    public void Parameters_outside_the_vocabulary_bounds_are_a_problem()
    {
        var graph = Graph(
            [Indicator("sma", BlockKind.Entry, "priceVsSma", days: 1000), Compare("positive", BlockKind.Entry, "gt", 0)],
            [Link("sma", "positive", "left"), Link("positive", "result-entry", "condition")]);

        var problem = Problems(graph).Single(found => found.Code == "builder.problem.parameter_out_of_range");

        problem.Message.ShouldBe("""@builder.problem.parameter_out_of_range {"field":"days","min":2,"max":400}""");
    }

    [Fact]
    public void A_required_parameter_is_a_problem_when_missing()
    {
        var graph = Graph(
            [Indicator("sma", BlockKind.Entry, "priceVsSma"), Compare("positive", BlockKind.Entry, "gt", 0)],
            [Link("sma", "positive", "left"), Link("positive", "result-entry", "condition")]);

        Codes(graph).ShouldContain("builder.problem.parameter_missing");
    }

    [Fact]
    public void The_fund_series_is_not_allowed_in_the_market_filter()
    {
        var graph = Graph(
            [Indicator("mom", BlockKind.MarketFilter, "blendedMomentum"), Compare("positive", BlockKind.MarketFilter, "gt", 0)],
            [Link("mom", "positive", "left"), Link("positive", "result-market", "condition")],
            NodeParameters.Empty.With("investWhenUnfavorable", 0.3));

        Codes(graph).ShouldContain("builder.problem.fund_in_market_filter");
    }

    [Fact]
    public void A_connected_market_filter_needs_its_share()
    {
        var graph = Graph(
            [Indicator("spy", BlockKind.MarketFilter, "momentum", "benchmark", months: 6), Compare("positive", BlockKind.MarketFilter, "gt", 0)],
            [Link("spy", "positive", "left"), Link("positive", "result-market", "condition")]);

        Codes(graph).ShouldBe(["builder.problem.market_filter_share_missing"]);
    }

    [Fact]
    public void A_this_fund_signal_is_not_allowed_in_the_market_filter()
    {
        var signal = new GraphNode(new NodeId("sig"), NodeKind.Signal, BlockKind.MarketFilter,
            NodeParameters.Empty.With("signalId", Guid.CreateVersion7().ToString()).With("state", "active").With("scope", "thisFund"));
        var graph = Graph([signal], [Link("sig", "result-market", "condition")], NodeParameters.Empty.With("investWhenUnfavorable", 0.3));

        Codes(graph).ShouldContain("builder.problem.this_fund_signal_in_market_filter");
    }

    [Fact]
    public void An_extra_series_must_be_among_the_chosen_ones()
    {
        var unknownExtra = Indicator("x", BlockKind.Entry, "price") with
        {
            Parameters = NodeParameters.Empty.With("indicator", "price").WithObject("of", new { series = "extra", instrumentId = Guid.CreateVersion7() }),
        };
        var graph = Graph(
            [unknownExtra, Compare("positive", BlockKind.Entry, "gt", 0)],
            [Link("x", "positive", "left"), Link("positive", "result-entry", "condition")]);

        Codes(graph).ShouldContain("builder.problem.extra_series_unknown");
    }

    [Fact]
    public void A_draft_needs_funds_a_benchmark_and_a_cash_fund()
    {
        var codes = Codes(MomentumEntry(), DraftSetup.Empty);

        codes.ShouldBe(["builder.problem.funds_missing", "builder.problem.benchmark_missing", "builder.problem.cash_missing"], ignoreOrder: true);
    }
}
