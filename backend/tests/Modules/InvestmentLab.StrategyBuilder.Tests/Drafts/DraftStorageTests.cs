using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Drafts.Infrastructure;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class DraftStorageTests
{
    private static T RoundTrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, DraftJson.Options), DraftJson.Options)!;

    [Fact]
    public void Manual_edge_routes_survive_storage_and_the_layout_api()
    {
        var link = new GraphLink(new NodeId("e-mom"), StrategyGraph.EntryResultId, NodeSockets.Condition);
        var points = new[] { new NodePosition(123.5, 64), new NodePosition(123.5, 200) };
        var layout = DraftLayout.Empty with { EdgeRoutes = new Dictionary<string, IReadOnlyList<NodePosition>> { [link.RouteKey()] = points } };
        var stored = RoundTrip(layout);
        stored.EdgeRoutes[link.RouteKey()].ShouldBe(points);
        var routes = new Dictionary<string, IReadOnlyList<NodePositionDto>> { [link.RouteKey()] = [new(123.5, 64), new(123.5, 200)] };
        var dto = new LayoutDto(new Dictionary<string, NodePositionDto>(), [], routes);
        DraftMapping.ToLayout(dto).Value.EdgeRoutes[link.RouteKey()].ShouldBe(points);
    }

    [Fact]
    public void Old_layout_json_without_routes_remains_readable()
    {
        var layout = JsonSerializer.Deserialize<DraftLayout>("{\"positions\":{},\"collapsed\":[]}", DraftJson.Options)!;
        layout.EdgeRoutes.ShouldBeEmpty();
    }

    [Fact]
    public void Removing_a_link_prunes_only_its_saved_route()
    {
        var a = new GraphLink(new NodeId("a"), StrategyGraph.EntryResultId, NodeSockets.Condition);
        var b = new GraphLink(new NodeId("b"), StrategyGraph.ExitResultId, NodeSockets.Condition);
        var points = new[] { new NodePosition(100, 100), new NodePosition(100, 200) };
        var layout = DraftLayout.Empty with { EdgeRoutes = new Dictionary<string, IReadOnlyList<NodePosition>> { [a.RouteKey()] = points, [b.RouteKey()] = points } };
        layout.Keeping(new StrategyGraph([], [a])).EdgeRoutes.Keys.ShouldBe([a.RouteKey()]);
    }

    [Fact]
    public void A_stored_graph_gives_the_same_rules()
    {
        var draft = Of(DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter));

        var restored = Of(RoundTrip(draft.Graph), RoundTrip(draft.Setup));

        RulesGenerator.Generate(restored).Json.ShouldBe(RulesGenerator.Generate(draft).Json);
    }

    [Fact]
    public void A_stored_layout_keeps_positions_and_folded_blocks()
    {
        var layout = new DraftLayout(new Dictionary<string, NodePosition> { ["e-mom"] = new(1.5, 2) }, [BlockKind.Risk]);

        var restored = RoundTrip(layout);

        restored.Positions["e-mom"].ShouldBe(new NodePosition(1.5, 2));
        restored.Collapsed.ShouldBe([BlockKind.Risk]);
    }
}
