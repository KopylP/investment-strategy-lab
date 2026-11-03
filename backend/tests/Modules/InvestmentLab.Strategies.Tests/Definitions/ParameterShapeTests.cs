using System.Text.Json;
using InvestmentLab.Strategies.Definitions.Application.Shared;

namespace InvestmentLab.Strategies.Tests.Definitions;

public sealed class ParameterShapeTests
{
    [Fact]
    public async Task A_matching_schema_alternative_does_not_report_errors_from_other_alternatives()
    {
        var checks = new AcceptedParameterChecks();
        var factory = new StrategySetupFactory(new AlternativeParameterTypes(), null!, checks);
        var parameters = JsonDocument.Parse("""{"value":5}""").RootElement;

        var result = await factory.CheckAsync("alternative", parameters, CancellationToken.None);

        result.Value.IsClean.ShouldBeTrue();
        checks.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task A_value_that_matches_no_schema_alternative_is_refused_before_engine_checks()
    {
        var checks = new AcceptedParameterChecks();
        var factory = new StrategySetupFactory(new AlternativeParameterTypes(), null!, checks);
        var parameters = JsonDocument.Parse("""{"value":false}""").RootElement;

        var result = await factory.CheckAsync("alternative", parameters, CancellationToken.None);

        result.Value.IsClean.ShouldBeFalse();
        result.Value.Problems.ShouldNotBeEmpty();
        checks.Calls.ShouldBe(0);
    }
}
