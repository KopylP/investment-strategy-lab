using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Drafts.Infrastructure;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class DraftStorageTests
{
    private static T RoundTrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, DraftJson.Options), DraftJson.Options)!;

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
