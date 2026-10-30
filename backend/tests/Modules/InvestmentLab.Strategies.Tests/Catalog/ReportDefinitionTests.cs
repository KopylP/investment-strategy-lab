using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Tests.Catalog;

/// <summary>A strategy type declares its reports; a strategy's parameters may say how often each one comes.</summary>
public sealed class ReportDefinitionTests
{
    private static readonly ReportDefinition Rebalance = new("rebalance", "Ребаланс", "", "rebalance", "monthly", "20:00", "reporting.rebalance");

    [Theory]
    [InlineData("""{"reporting":{"rebalance":"weekly"}}""", "weekly")]
    [InlineData("""{"reporting":{"rebalance":"off"}}""", "off")]
    [InlineData("""{"reporting":{}}""", "monthly")]                  // not set: the type's default
    [InlineData("""{"reporting":{"rebalance":"daily"}}""", "monthly")] // not a frequency
    [InlineData("""{"reporting":"weekly"}""", "monthly")]
    [InlineData("not json", "monthly")]
    public void The_strategys_parameter_sets_the_frequency(string parameters, string expected) =>
        Rebalance.FrequencyFor(parameters).ShouldBe(expected);

    [Fact]
    public void Without_a_frequency_parameter_the_declared_frequency_holds() =>
        (Rebalance with { FrequencyParameter = null }).FrequencyFor("""{"reporting":{"rebalance":"weekly"}}""").ShouldBe("monthly");
}
