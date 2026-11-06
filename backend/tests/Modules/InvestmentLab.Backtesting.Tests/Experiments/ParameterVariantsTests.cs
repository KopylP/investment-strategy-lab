using System.Text.Json;
using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ParameterVariantsTests
{
    private const string Parameters = """{"selection":{"top":3},"entry":{"conditions":[{"level":0.1}]}}""";

    [Fact]
    public void The_original_candidate_stays_first_and_repeated_values_are_removed()
    {
        var neighborhood = new ParameterNeighborhood("selection.top", [4, 3, 2, 4]);
        var variants = ParameterVariants.Of(Parameters, neighborhood).Value;

        variants.Select(candidate => candidate.Value).ShouldBe([3m, 4m, 2m]);
        var changed = JsonDocument.Parse(variants[1].Parameters).RootElement;
        changed.GetProperty("selection").GetProperty("top").GetInt32().ShouldBe(4);
    }

    [Fact]
    public void Numeric_parameters_can_be_changed_inside_nested_condition_arrays()
    {
        var neighborhood = new ParameterNeighborhood("entry.conditions.0.level", [.2m]);
        var variants = ParameterVariants.Of(Parameters, neighborhood).Value;
        var changed = JsonDocument.Parse(variants[1].Parameters).RootElement;

        changed.GetProperty("entry").GetProperty("conditions")[0].GetProperty("level").GetDecimal().ShouldBe(.2m);
    }

    [Theory]
    [InlineData("selection.missing")]
    [InlineData("entry.conditions.-1.level")]
    [InlineData("entry.conditions.2.level")]
    [InlineData("entry.conditions")]
    public void Missing_and_non_numeric_parameter_paths_are_refused(string path)
    {
        var neighborhood = new ParameterNeighborhood(path, [2]);

        ParameterVariants.Of(Parameters, neighborhood).Error.Code.ShouldBe("research.parameter_invalid");
    }

    [Fact]
    public void A_plan_without_sensitivity_preserves_the_original_parameters_exactly()
    {
        var candidate = ParameterVariants.Of(Parameters, null).Value.ShouldHaveSingleItem();

        candidate.Parameters.ShouldBe(Parameters);
        candidate.Value.ShouldBeNull();
    }
}
