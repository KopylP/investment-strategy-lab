using System.Text.Json.Nodes;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class SetupBindingsTests
{
    private static StrategyGraph Bound(string output) => MomentumEntry() with { Links = [.. MomentumEntry().Links, new GraphLink(StrategyGraph.SetupId, new NodeId("mom"), "of", output)] };

    [Fact]
    public void The_same_funds_output_supplies_candidates_and_each_funds_price_series()
    {
        var graph = DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter);
        graph.OutOf(StrategyGraph.SetupId).Where(link => link.Input == NodeSockets.Funds).ShouldHaveSingleItem().Output.ShouldBe(NodeSockets.Funds);
        graph.OutOf(StrategyGraph.SetupId).ShouldContain(link => link.Output == NodeSockets.Funds && link.Input == NodeSockets.Of);
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldBeEmpty();
    }

    [Fact]
    public void The_old_separate_universe_output_is_merged_without_changing_rules()
    {
        var graph = DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter);
        var old = graph with { Links = [.. graph.Links.Select(link => link.From == StrategyGraph.SetupId && link.Input == NodeSockets.Funds ? link with { Output = "universe" } : link)] };
        var merged = PortfolioFlow.Upgrade(old);
        merged.Links.ShouldNotContain(link => link.Output == "universe");
        RulesGenerator.Generate(Of(merged)).Json.ShouldBe(RulesGenerator.Generate(Of(graph)).Json);
        PortfolioFlow.Upgrade(merged).ShouldBeSameAs(merged);
        GraphCheck.Of(Of(merged), EngineSchema.Vocabulary).ShouldBeEmpty();
    }

    [Fact]
    public void Every_empty_graph_has_one_permanent_setup_node() =>
        StrategyGraph.Empty.Nodes.Single(node => node.Kind == NodeKind.Setup).Id.ShouldBe(StrategyGraph.SetupId);

    [Fact]
    public void An_api_payload_cannot_delete_the_setup_node()
    {
        var graph = StrategyGraph.Empty;
        var payload = new GraphDto([.. graph.Nodes.Where(node => node.Kind != NodeKind.Setup).Select(node => new NodeDto(node.Id.Value, node.Kind.ToString(), node.Block.ToString(), node.Parameters.Values))], []);
        var mapped = DraftMapping.ToChange(new DraftChangeDto(1, null, null, null, payload, null)).Value.Graph!;
        mapped.Nodes.ShouldContain(node => node.Id == StrategyGraph.SetupId && node.Kind == NodeKind.Setup);
    }

    [Fact]
    public void The_trend_template_can_be_saved_with_multiple_funds_and_the_shared_setup_node()
    {
        var draft = Of(DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter));
        var dto = DraftMapping.ToDto(draft, [], []);
        var funds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var change = new DraftChangeDto(dto.Revision, null, null, dto.Setup with { Funds = funds }, dto.Graph, null);

        var result = DraftMapping.ToChange(change);

        result.IsFailure.ShouldBeFalse();
        result.Value.Setup!.Funds.ShouldBe(funds);
        result.Value.Graph!.Nodes.Single(node => node.Id == StrategyGraph.SetupId).Kind.ShouldBe(NodeKind.Setup);
        result.Value.Graph.Links.ShouldContain(link => link.From == StrategyGraph.SetupId && link.Output == "funds");
        result.Value.Graph.Links.ShouldContain(link => link.From == StrategyGraph.SetupId && link.Output == "benchmarkId");
    }

    [Fact]
    public void The_selected_setup_socket_overrides_a_stale_local_series()
    {
        var graph = Bound("benchmarkId");
        graph = graph with { Links = [.. graph.Links, Link("positive", "result-market", "condition")] };
        graph = graph.WithNode(graph.ResultOf(BlockKind.MarketFilter) with { Parameters = NodeParameters.Empty.With("investWhenUnfavorable", 0) });
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldBeEmpty();
        var rules = JsonNode.Parse(RulesGenerator.Generate(Of(graph)).Json)!;
        rules["indicators"]!["blendedMomentumBenchmark"]!["of"]!["series"]!.ToString().ShouldBe("benchmark");
    }

    [Fact]
    public void A_fund_binding_cannot_be_hidden_behind_a_benchmark_parameter()
    {
        var graph = Bound("funds");
        graph = graph.WithNode(graph.Node(new NodeId("mom"))! with { Parameters = NodeParameters.Empty.With("indicator", "blendedMomentum").WithObject("of", new { series = "benchmark" }) });
        graph = graph with { Links = [.. graph.Links, Link("positive", "result-market", "condition")] };
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldContain(problem => problem.Code == "builder.problem.fund_in_market_filter");
    }

    [Fact]
    public void Missing_optional_sources_and_unknown_outputs_are_rejected()
    {
        GraphCheck.Of(Of(Bound("volatilityIndexId")), EngineSchema.Vocabulary).ShouldContain(problem => problem.Code == "builder.problem.parameter_missing");
        GraphCheck.Of(Of(Bound("unknown")), EngineSchema.Vocabulary).ShouldContain(problem => problem.Code == "builder.problem.socket_mismatch");
        GraphCheck.Of(Of(Bound("extraSeries")), EngineSchema.Vocabulary).ShouldContain(problem => problem.Code == "builder.problem.socket_mismatch");
    }

    [Fact]
    public void An_indicator_can_have_only_one_series_binding()
    {
        var graph = Bound("funds");
        graph = graph with { Links = [.. graph.Links, new GraphLink(StrategyGraph.SetupId, new NodeId("mom"), "of", "benchmarkId")] };
        GraphCheck.Of(Of(graph), EngineSchema.Vocabulary).ShouldContain(problem => problem.Code == "builder.problem.socket_mismatch");
    }

    [Fact]
    public void Cash_binding_supplies_its_price_series_to_execution()
    {
        var draft = Of(Bound("cashId"));
        var setup = SetupBindings.EvaluationSetup(draft);
        setup.ExtraSeries.ShouldContain(Cash);
        var indicators = JsonNode.Parse(RulesGenerator.Generate(draft).Json)!["indicators"]!.AsObject();
        indicators.Single().Value!["of"]!["instrumentId"]!.ToString().ShouldBe(Cash.ToString());
        GraphCheck.Of(draft, EngineSchema.Vocabulary).ShouldBeEmpty();
    }

    [Fact]
    public void Legacy_migration_preserves_rules_and_does_not_recreate_removed_bindings()
    {
        var legacy = MomentumEntry() with { Nodes = [.. MomentumEntry().Nodes.Where(node => node.Kind != NodeKind.Setup)] };
        var migrated = SetupBindings.WithLegacyBindings(legacy);
        migrated.Into(new NodeId("mom"), "of").ShouldHaveSingleItem().Output.ShouldBe("funds");
        RulesGenerator.Generate(Of(migrated)).Json.ShouldBe(RulesGenerator.Generate(Of(legacy)).Json);
        var detached = migrated with { Links = [.. migrated.Links.Where(link => link.Input != "of")] };
        SetupBindings.WithLegacyBindings(detached).Links.ShouldBe(detached.Links);
    }

    [Fact]
    public void Setup_output_names_survive_storage_round_trip()
    {
        var graph = Bound("benchmarkId");
        var json = System.Text.Json.JsonSerializer.Serialize(graph, InvestmentLab.StrategyBuilder.Drafts.Infrastructure.DraftJson.Options);
        var restored = System.Text.Json.JsonSerializer.Deserialize<StrategyGraph>(json, InvestmentLab.StrategyBuilder.Drafts.Infrastructure.DraftJson.Options)!;
        restored.Into(new NodeId("mom"), "of").ShouldHaveSingleItem().Output.ShouldBe("benchmarkId");
        RulesGenerator.Generate(Of(restored)).Json.ShouldBe(RulesGenerator.Generate(Of(graph)).Json);
    }
}
