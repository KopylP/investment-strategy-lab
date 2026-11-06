using System.Text.Json.Nodes;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class PortfolioFlowTests
{
    private static StrategyGraph Template() => DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter);

    [Theory]
    [InlineData(DraftTemplates.MomentumTrendFilter)]
    [InlineData(DraftTemplates.DualMomentum)]
    [InlineData(DraftTemplates.IndexHold)]
    public void Every_template_converges_into_one_complete_portfolio(string template)
    {
        var graph = DraftTemplates.GraphOf(template);
        graph.Nodes.Where(node => !graph.OutOf(node.Id).Any()).Select(node => node.Id).ShouldBe([PortfolioFlow.PortfolioId]);
        graph.Into(PortfolioFlow.PortfolioId).Select(link => link.Input).Order().ShouldBe(new[] { NodeSockets.Cash, NodeSockets.Funds, NodeSockets.Risk }.Order());
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldBeEmpty();
    }

    [Fact]
    public void Completing_an_existing_flow_preserves_rules_and_is_idempotent()
    {
        var graph = Template();
        var previous = graph with
        {
            Nodes = [.. graph.Nodes.Where(node => node.Id != PortfolioFlow.PortfolioId)],
            Links = [.. graph.Links.Where(link => link.To != PortfolioFlow.PortfolioId)],
        };
        var completed = PortfolioFlow.Upgrade(previous);
        RulesGenerator.Generate(Of(completed)).Json.ShouldBe(RulesGenerator.Generate(Of(previous)).Json);
        PortfolioFlow.Upgrade(completed).ShouldBeSameAs(completed);
        completed.Links.ShouldContain(link => link.From == PortfolioFlow.CashId && link.To == PortfolioFlow.PortfolioId);
    }

    [Fact]
    public void An_explicitly_disconnected_portfolio_branch_is_reported_instead_of_silently_reconnected()
    {
        var graph = Template();
        graph = graph with { Links = [.. graph.Links.Where(link => !(link.To == PortfolioFlow.PortfolioId && link.Input == NodeSockets.Funds))] };
        var problems = GraphCheck.Of(Of(graph), EngineSchema.Vocabulary);
        problems.ShouldContain(problem => problem.Node == PortfolioFlow.PortfolioId && problem.Code == "builder.problem.input_missing");
    }

    [Fact]
    public void Selection_weights_and_cash_are_separate_modules_with_typed_links()
    {
        var graph = Template();
        var selector = PortfolioFlow.Selector(graph)!;
        selector.Parameters.Integer("top").ShouldBe(3);
        selector.Parameters.Has("method").ShouldBeFalse();
        graph.ResultOf(BlockKind.Selection).Parameters.Has("top").ShouldBeFalse();
        graph.Into(PortfolioFlow.CashId, NodeSockets.Remainder).Single().Output.ShouldBe("cash");
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldBeEmpty();
    }

    [Fact]
    public void Legacy_graph_migration_preserves_the_complete_generated_strategy()
    {
        var graph = Template();
        var weights = graph.ResultOf(BlockKind.Selection);
        var selector = PortfolioFlow.Selector(graph)!;
        foreach (var (name, value) in selector.Parameters.Values) weights = weights with { Parameters = weights.Parameters.With(name, value) };
        var legacy = new StrategyGraph(
            [.. graph.Nodes.Where(node => node.Kind is not (NodeKind.Cash or NodeKind.FundSelection)).Select(node => node.Id == weights.Id ? weights : node)],
            [.. graph.Links.Where(link => link.Input is not (NodeSockets.Funds or NodeSockets.Budget or NodeSockets.Remainder)).Select(link => link.To == selector.Id ? link with { To = weights.Id } : link)]);

        RulesGenerator.Generate(Of(legacy)).Json.ShouldBe(RulesGenerator.Generate(Of(graph)).Json);
        var migrated = PortfolioFlow.Upgrade(legacy);
        PortfolioFlow.Upgrade(migrated).ShouldBeSameAs(migrated);
    }

    [Theory]
    [InlineData(BlockKind.Entry, "entry")]
    [InlineData(BlockKind.Exit, "exit")]
    public void Bypassing_a_gate_removes_its_rule_from_execution(BlockKind block, string section)
    {
        var graph = Template();
        var gate = graph.ResultOf(block);
        var input = graph.Into(gate.Id, NodeSockets.Funds).Single();
        var output = graph.OutOf(gate.Id).Single();
        graph = graph with { Links = [.. graph.Links.Where(link => link != output), output with { From = input.From, Output = input.Output }] };

        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldNotContain(problem => problem.Severity == ProblemSeverity.Error);
        JsonNode.Parse(RulesGenerator.Generate(Of(graph)).Json)!.AsObject().ContainsKey(section).ShouldBeFalse();
        StrategyInWords.Of(Of(graph), EngineSchema.Vocabulary, InstrumentLabels.None).ShouldNotContain(words => words.Block == block);
    }

    [Fact]
    public void Bypassing_selection_uses_all_available_candidates()
    {
        var graph = Template();
        var connection = graph.Into(StrategyGraph.SelectionResultId, NodeSockets.Funds).Single();
        graph = graph with { Links = [.. graph.Links.Where(link => link != connection), connection with { From = StrategyGraph.EntryResultId }] };
        var rules = JsonNode.Parse(RulesGenerator.Generate(Of(graph)).Json)!;
        rules["selection"]!["top"]!.GetValue<int>().ShouldBe(Setup.Funds.Count);
        rules["selection"]!.AsObject().ContainsKey("rankBy").ShouldBeFalse();
    }

    [Fact]
    public void Only_the_remainder_after_weights_can_feed_cash()
    {
        var graph = Template();
        graph = graph with { Links = [.. graph.Links.Select(link => link.To == PortfolioFlow.CashId ? link with { From = StrategyGraph.MarketResultId, Output = "output" } : link)] };
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldContain(problem => problem.Node == PortfolioFlow.CashId && problem.Code == "builder.problem.socket_mismatch");
    }

    [Fact]
    public void The_permanent_cash_module_cannot_be_deleted_by_an_api_payload()
    {
        var draft = Of(Template());
        var dto = DraftMapping.ToDto(draft, [], []);
        var graph = dto.Graph with { Nodes = [.. dto.Graph.Nodes.Where(node => node.Kind != "Cash")] };
        var mapped = DraftMapping.ToChange(new DraftChangeDto(dto.Revision, null, null, null, graph, null));
        mapped.IsFailure.ShouldBeFalse();
        mapped.Value.Graph!.Nodes.ShouldContain(node => node.Id == PortfolioFlow.CashId && node.Kind == NodeKind.Cash);
    }

    [Fact]
    public void Cash_price_series_is_an_external_input_even_when_the_cash_budget_is_a_sink()
    {
        var graph = Template();
        graph = graph with { Links = [.. graph.Links.Select(link => link.To.Value == "m-price" && link.Input == "of" ? link with { From = PortfolioFlow.CashId, Output = "series" } : link)] };
        var problems = GraphCheck.Of(Of(graph), EngineSchema.Vocabulary);
        problems.ShouldNotContain(problem => problem.Code == "builder.problem.cycle" || problem.Code == "builder.problem.fund_in_market_filter");
    }
}
