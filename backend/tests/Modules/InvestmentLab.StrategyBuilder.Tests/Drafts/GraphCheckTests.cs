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
    public void Shared_indicators_can_feed_conditions_from_different_legacy_blocks()
    {
        var graph = Graph(
            [Indicator("mom", BlockKind.Exit, "blendedMomentum"), Compare("positive", BlockKind.Entry, "gt", 0)],
            [Link("mom", "positive", "left"), Link("positive", "result-entry", "condition")]);

        Problems(graph).ShouldBeEmpty();
    }

    [Fact]
    public void One_condition_can_feed_entry_and_exit_and_one_indicator_can_rank_selection()
    {
        var graph = MomentumEntry();
        graph = graph with { Links = [.. graph.Links, Link("positive", "result-exit", "condition"), Link("mom", "result-selection", "rankBy")] };
        Problems(graph).ShouldBeEmpty();
    }

    [Fact]
    public void Market_context_is_checked_through_shared_nested_conditions()
    {
        var graph = Graph(
            [Indicator("fund", BlockKind.Shared, "price"), Compare("up", BlockKind.Shared, "gt", 100), Logic("all", BlockKind.Shared, NodeKind.All)],
            [Link("fund", "up", "left"), Link("up", "all", "conditions"), Link("all", "result-entry", "condition"), Link("all", "result-market", "condition")],
            NodeParameters.Empty.With("investWhenUnfavorable", 0));
        Problems(graph).ShouldContain(problem => problem.Code == "builder.problem.fund_in_market_filter" && problem.Node == new NodeId("fund"));
    }

    [Fact]
    public void A_benchmark_condition_can_be_shared_by_market_and_entry()
    {
        var graph = Graph(
            [Indicator("market", BlockKind.Shared, "price", "benchmark"), Compare("up", BlockKind.Shared, "gt", 100)],
            [Link("market", "up", "left"), Link("up", "result-entry", "condition"), Link("up", "result-market", "condition")],
            NodeParameters.Empty.With("investWhenUnfavorable", 0));
        Problems(graph).ShouldBeEmpty();
    }

    [Fact]
    public void A_shared_fund_signal_cannot_indirectly_feed_a_market_filter()
    {
        var id = Guid.NewGuid();
        var signal = new GraphNode(new NodeId("signal"), NodeKind.Signal, BlockKind.Shared,
            NodeParameters.Empty.With("signalId", id.ToString()).With("state", "active").With("scope", "thisFund"));
        var graph = Graph([signal, Logic("not", BlockKind.Shared, NodeKind.Not)],
            [Link("signal", "not", "condition"), Link("not", "result-market", "condition")], NodeParameters.Empty.With("investWhenUnfavorable", 0));
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary, [new OwnSignal(id, "Signal", true, "Instrument")])
            .ShouldContain(problem => problem.Code == "builder.problem.this_fund_signal_in_market_filter");
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
    private static StrategyDraft SignalDraft(Guid id, bool inReport = false)
    {
        var node = new GraphNode(new NodeId("signal"), NodeKind.Signal, BlockKind.Entry,
            NodeParameters.Empty.With("signalId", id.ToString()).With("state", "active").With("scope", "thisFund"));
        var graph = inReport ? MomentumEntry() : Graph([node], [Link("signal", "result-entry", "condition")]);
        DraftReport[] reports = inReport ? [new DraftReport("risk-review", new ReportTiming("off", 1, "Monday", "12:00", 0), [], [id])] : [];

        return StrategyDraft.Start(UserId.New(), "Signal draft", Setup, graph, reports, null, Now).Value;
    }

    [Fact]
    public void A_deleted_signal_in_the_rules_is_a_problem_on_its_node()
    {
        var draft = SignalDraft(Guid.NewGuid());

        GraphCheck.Of(draft, EngineSchema.Vocabulary, []).ShouldContain(problem =>
            problem.Code == "builder.problem.signal_deleted" && problem.Node == new NodeId("signal"));
    }

    [Fact]
    public void A_signal_of_someone_else_is_treated_as_missing()
    {
        var draft = SignalDraft(Guid.NewGuid());
        OwnSignal[] own = [new OwnSignal(Guid.NewGuid(), "Own", true, "Instrument")];

        GraphCheck.Of(draft, EngineSchema.Vocabulary, own).ShouldContain(problem => problem.Code == "builder.problem.signal_deleted");
    }

    [Fact]
    public void A_position_signal_cannot_be_a_condition()
    {
        var id = Guid.NewGuid();

        GraphCheck.Of(SignalDraft(id), EngineSchema.Vocabulary, [new OwnSignal(id, "Position", false, "EachPosition")])
            .ShouldContain(problem => problem.Code == "builder.problem.signal_not_market");
    }

    [Fact]
    public void A_deleted_signal_in_a_report_is_a_problem_of_that_report()
    {
        GraphCheck.Of(SignalDraft(Guid.NewGuid(), inReport: true), EngineSchema.Vocabulary, [])
            .ShouldContain(problem => problem.Code == "builder.problem.signal_deleted" && problem.ReportCode == "risk-review");
    }

    [Fact]
    public void A_position_signal_as_a_report_trigger_is_a_warning()
    {
        var id = Guid.NewGuid();
        var problems = GraphCheck.Of(SignalDraft(id, inReport: true), EngineSchema.Vocabulary,
            [new OwnSignal(id, "Position", false, "EachPosition")]);

        problems.ShouldContain(problem => problem.Code == "builder.problem.trigger_differs_in_backtest" && problem.Severity == ProblemSeverity.Warning);
        DraftCheck.HasErrors(problems).ShouldBeFalse();
    }

    [Fact]
    public void A_draft_remembers_a_deleted_signal()
    {
        var id = Guid.NewGuid();
        var draft = SignalDraft(id);
        draft.MarkSignalMissing(id);
        draft.MarkSignalMissing(id);

        draft.MissingSignalIds.ShouldBe([id]);
        GraphCheck.Of(draft, EngineSchema.Vocabulary, [new OwnSignal(id, "Stale", true, "Instrument")])
            .ShouldContain(problem => problem.Code == "builder.problem.signal_deleted");
    }

}
