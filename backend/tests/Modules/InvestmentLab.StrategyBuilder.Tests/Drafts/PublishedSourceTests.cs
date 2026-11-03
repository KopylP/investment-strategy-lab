using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class PublishedSourceTests
{
    [Fact]
    public void The_saved_layout_of_the_same_version_is_used()
    {
        var draft = Of(MomentumEntry());
        var layout = new DraftLayout(new Dictionary<string, NodePosition> { ["mom"] = new(32, 64) }, [BlockKind.Risk]);
        var saved = PublishedLayout.Of(draft.OwnerId, new PublishedVersion(Guid.CreateVersion7(), 2), draft.Graph, layout);
        var version = VersionOf(draft, 2);

        var chosen = PublishedSource.Choose(saved, version, EngineSchema.Vocabulary).Value;

        chosen.Graph.ShouldBe(draft.Graph);
        chosen.Layout.ShouldBe(layout);
    }

    [Fact]
    public void Another_version_is_rebuilt_from_its_rules()
    {
        var draft = Of(MomentumEntry());
        var saved = PublishedLayout.Of(draft.OwnerId, new PublishedVersion(Guid.CreateVersion7(), 1), StrategyGraph.Empty, DraftLayout.Empty);

        var chosen = PublishedSource.Choose(saved, VersionOf(draft, 2), EngineSchema.Vocabulary).Value;

        chosen.Graph.Nodes.ShouldContain(node => node.Kind == NodeKind.Compare);
        chosen.Layout.ShouldBe(DraftLayout.Empty);
    }

    [Fact]
    public void A_version_without_a_saved_layout_is_rebuilt_from_its_rules()
    {
        var draft = Of(MomentumEntry());

        var chosen = PublishedSource.Choose(null, VersionOf(draft, 1), EngineSchema.Vocabulary).Value;

        chosen.Graph.Nodes.ShouldContain(node => node.Kind == NodeKind.Compare);
        chosen.Layout.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void Unreadable_published_rules_are_refused()
    {
        var draft = Of(MomentumEntry());
        var version = VersionOf(draft, 1) with { Rules = "invalid" };

        PublishedSource.Choose(null, version, EngineSchema.Vocabulary).IsFailure.ShouldBeTrue();
    }

    private static AuthoredVersion VersionOf(StrategyDraft draft, int version) => new(
        Name: draft.Name,
        Rules: RulesGenerator.Generate(draft).Json,
        Setup: draft.Setup,
        Reports: draft.Reports,
        TypeCode: RuleBasedType.Code,
        Version: version);
}
