using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class DraftTemplatesTests
{
    [Theory]
    [InlineData(DraftTemplates.MomentumTrendFilter)]
    [InlineData(DraftTemplates.DualMomentum)]
    [InlineData(DraftTemplates.IndexHold)]
    public void Every_template_is_a_valid_draft(string code)
    {
        var problems = GraphCheck.Of(Of(DraftTemplates.GraphOf(code)), EngineSchema.Vocabulary);

        problems.Where(problem => problem.Severity == ProblemSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    public void Templates_leave_no_node_unused()
    {
        var problems = DraftTemplates.Codes.SelectMany(code => GraphCheck.Of(Of(DraftTemplates.GraphOf(code)), EngineSchema.Vocabulary));

        problems.ShouldBeEmpty();
    }
}
