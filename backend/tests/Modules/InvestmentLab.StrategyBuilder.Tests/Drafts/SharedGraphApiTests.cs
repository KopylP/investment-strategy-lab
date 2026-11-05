using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class SharedGraphApiTests
{
    private static GraphDto Payload(StrategyGraph graph) => new(
        graph.Nodes.Select(node => new NodeDto(node.Id.Value, node.Kind.ToString(), node.Block.ToString(), node.Parameters.Values)).ToList(),
        graph.Links.Select(link => new LinkDto(link.From.Value, link.To.Value, link.Input)).ToList());

    [Fact]
    public void A_legacy_payload_becomes_shared_and_checks_all_reachable_results()
    {
        var source = MomentumEntry();
        source = source.WithNode(source.ResultOf(BlockKind.MarketFilter) with { Parameters = NodeParameters.Empty.With("investWhenUnfavorable", 0) });
        source = source with { Links = [.. source.Links, Link("positive", "result-market", "condition"), Link("mom", "result-selection", "rankBy")] };
        var changed = DraftMapping.ToChange(new DraftChangeDto(1, null, null, null, Payload(source), null)).Value.Graph!;
        changed.Nodes.Where(node => !NodeKinds.IsResult(node.Kind)).ShouldAllBe(node => node.Block == BlockKind.Shared);
        GraphCheck.Of(Of(changed), EngineSchema.Vocabulary).ShouldContain(problem => problem.Code == "builder.problem.fund_in_market_filter");
    }

    [Fact]
    public void Shared_payloads_round_trip_without_losing_fan_out()
    {
        var source = MomentumEntry();
        source = source with { Links = [.. source.Links, Link("positive", "result-exit", "condition")] };
        var graph = DraftMapping.ToChange(new DraftChangeDto(1, null, null, null, Payload(source), null)).Value.Graph!;
        var dto = DraftMapping.ToDto(Of(graph), [], []);
        dto.Graph.Links.Count(link => link.From == "positive").ShouldBe(2);
        dto.Graph.Nodes.Where(node => !node.Kind.EndsWith("Result")).ShouldAllBe(node => node.Block == "Shared");
        var reread = DraftMapping.ToChange(new DraftChangeDto(1, null, null, null, dto.Graph, null)).Value.Graph!;
        RulesGenerator.Generate(Of(reread)).Json.ShouldBe(RulesGenerator.Generate(Of(source)).Json);
    }

    [Fact]
    public void A_result_cannot_claim_a_different_context()
    {
        var source = MomentumEntry();
        source = source.WithNode(source.ResultOf(BlockKind.MarketFilter) with { Block = BlockKind.Entry });
        DraftMapping.ToChange(new DraftChangeDto(1, null, null, null, Payload(source), null)).IsFailure.ShouldBeTrue();
    }
}
