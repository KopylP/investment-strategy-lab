using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class StrategyDraftTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    private static StrategyDraft Draft(StrategyGraph? graph = null) =>
        StrategyDraft.Start(UserId.New(), "Momentum", DraftSetup.Empty, graph ?? StrategyGraph.Empty, [], null, Now).Value;

    private static GraphNode Price(string id) =>
        new(new NodeId(id), NodeKind.Indicator, BlockKind.Entry, NodeParameters.Empty.With("indicator", "price"));

    private static DraftChange GraphChange(int revision, StrategyGraph graph) => new(revision, null, null, null, graph, null);

    [Fact]
    public void A_repeated_run_returns_the_existing_trial()
    {
        var draft = Draft();
        var strategyId = Guid.CreateVersion7();
        var first = draft.RecordTrial(strategyId, 1, "fingerprint-one", "First", Now);

        draft.TrialWith("fingerprint-one").ShouldBe(first);
        draft.TrialWith("fingerprint-two").ShouldBeNull();
    }

    [Fact]
    public void Trial_numbers_count_up_and_the_first_run_remembers_the_trial_strategy()
    {
        var draft = Draft();
        var strategyId = Guid.CreateVersion7();

        var first = draft.RecordTrial(strategyId, 1, "fingerprint-one", null, Now);
        var second = draft.RecordTrial(strategyId, 2, "fingerprint-two", "Second", Now.AddMinutes(1));

        first.Number.ShouldBe(1);
        second.Number.ShouldBe(2);
        second.StrategyVersion.ShouldBe(2);
        second.Label.ShouldBe("Second");
        draft.TrialStrategyId.ShouldBe(strategyId);
        draft.Trials.ShouldBe([first, second]);
        draft.Revision.ShouldBe(1);
    }

    [Fact]
    public void A_new_draft_starts_at_revision_one_with_an_empty_layout()
    {
        var draft = Draft();

        draft.Revision.ShouldBe(1);
        draft.Layout.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void A_blank_name_is_refused() =>
        StrategyDraft.Start(UserId.New(), " ", DraftSetup.Empty, StrategyGraph.Empty, [], null, Now).Error.Code.ShouldBe("builder.draft.name_required");

    [Fact]
    public void A_change_based_on_an_old_revision_is_refused()
    {
        var draft = Draft();

        draft.Change(GraphChange(0, StrategyGraph.Empty), Now).Error.Code.ShouldBe("builder.draft.stale");
        draft.Change(GraphChange(1, StrategyGraph.Empty), Now).IsSuccess.ShouldBeTrue();
        draft.Revision.ShouldBe(2);
    }

    [Fact]
    public void A_new_graph_forgets_positions_of_removed_nodes()
    {
        var both = StrategyGraph.Empty with { Nodes = [.. StrategyGraph.Empty.Nodes, Price("n1"), Price("n2")] };
        var draft = Draft(both);
        draft.MoveNodes(new DraftLayout(new Dictionary<string, NodePosition> { ["n1"] = new(1, 2), ["n2"] = new(3, 4) }, []), Now);

        draft.Change(GraphChange(1, StrategyGraph.Empty with { Nodes = [.. StrategyGraph.Empty.Nodes, Price("n1")] }), Now);

        draft.Layout.Positions.Keys.ShouldBe(["n1"]);
    }

    [Fact]
    public void Moving_nodes_does_not_count_as_a_change_of_rules()
    {
        var draft = Draft();

        draft.MoveNodes(new DraftLayout(new Dictionary<string, NodePosition> { ["result-entry"] = new(5, 5) }, [BlockKind.Risk]), Now);

        draft.Revision.ShouldBe(1);
        draft.Layout.Collapsed.ShouldBe([BlockKind.Risk]);
    }

    [Fact]
    public void A_draft_takes_at_most_sixty_funds()
    {
        var funds = Enumerable.Range(0, 61).Select(_ => Guid.CreateVersion7()).ToList();

        DraftSetup.TryOf(funds, null, null, null, []).Error.Code.ShouldBe("builder.setup.too_many_funds");
    }

    [Fact]
    public void A_fund_may_appear_once()
    {
        var fund = Guid.CreateVersion7();

        DraftSetup.TryOf([fund, fund], null, null, null, []).Error.Code.ShouldBe("builder.setup.fund_repeated");
    }

    [Fact]
    public void The_empty_graph_has_one_result_per_block()
    {
        var results = StrategyGraph.Empty.Nodes.Select(node => node.Block).ToList();

        results.ShouldBe([BlockKind.MarketFilter, BlockKind.Exit, BlockKind.Entry, BlockKind.Selection, BlockKind.Risk]);
    }
}
