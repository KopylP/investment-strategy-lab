using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class EngineProblemPlacementTests
{
    private static readonly StrategyDraft Draft = Of(DraftTemplates.GraphOf(DraftTemplates.MomentumTrendFilter));

    private static DraftProblem Placed(string path)
    {
        var rules = RulesGenerator.Generate(Draft);
        var problem = new EngineProblem(path, "rule_based.check.indicator_unknown", """@rule_based.check.indicator_unknown {"name":"x"}""");

        return EngineProblems.Placed([problem], rules.SourceMap, Draft.Graph).ShouldHaveSingleItem();
    }

    [Fact]
    public void An_engine_problem_lands_on_the_node_of_its_path()
    {
        var problem = Placed("exit.condition.level");

        problem.Node.ShouldBe(new NodeId("x-cross"));
        problem.Block.ShouldBe(BlockKind.Exit);
        problem.Message.ShouldBe("""@rule_based.check.indicator_unknown {"name":"x"}""");
    }

    [Fact]
    public void An_engine_problem_outside_any_node_belongs_to_the_draft()
    {
        var problem = Placed("formatVersion");

        problem.Node.ShouldBeNull();
        problem.Block.ShouldBeNull();
    }
}
