using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Tests.Definitions;

public sealed class StrategyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Spy = Guid.CreateVersion7();

    private static readonly StrategyTypeDescriptor Type = StrategyTypeDescriptor.Of("etf-momentum", "ETF momentum", "", "python", "1", false, ["instrument"],
        """
        {"type":"object","additionalProperties":false,
         "properties":{"scanner":{"$ref":"#/$defs/Scanner"},"weakMarketInvestPct":{"type":"number","minimum":0,"maximum":1}},
         "$defs":{"Scanner":{"type":"object","additionalProperties":false,
           "properties":{"topN":{"type":"integer","minimum":1,"maximum":20},"maPeriod":{"type":"integer","minimum":20}}}}}
        """,
        """{"scanner":{"topN":4,"maPeriod":200},"weakMarketInvestPct":0.5}""", [], Now);

    private static StrategySetup Setup(params Guid[] universe) =>
        new("{}", universe, null, new MarketSeriesRefs(Spy, null));

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Reconfiguring_adds_a_version_and_keeps_the_old_one()
    {
        var first = Guid.CreateVersion7();
        var strategy = Strategy.Create(UserId.New(), "EU", "etf-momentum", Setup(first), Now).Value;

        strategy.Reconfigure(Setup(first, Guid.CreateVersion7()), Now.AddDays(1)).Value.Number.ShouldBe(2);

        strategy.Latest.Universe.Count.ShouldBe(2);
        strategy.Version(1)!.Universe.ShouldBe([first]);
        strategy.Version(3).ShouldBeNull();
    }

    [Fact]
    public void An_empty_or_repeated_universe_is_refused()
    {
        var instrument = Guid.CreateVersion7();

        Strategy.Create(UserId.New(), "EU", "etf-momentum", Setup(), Now).Error.Code.ShouldBe("strategy.universe_empty");
        Strategy.Create(UserId.New(), "EU", "etf-momentum", Setup(instrument, instrument), Now).Error.Code.ShouldBe("strategy.universe_repeated");
    }

    [Fact]
    public void An_archived_strategy_cannot_be_reconfigured()
    {
        var strategy = Strategy.Create(UserId.New(), "EU", "etf-momentum", Setup(Guid.CreateVersion7()), Now).Value;
        strategy.Archive();

        strategy.Reconfigure(Setup(Guid.CreateVersion7()), Now).Error.Code.ShouldBe("strategy.archived");
    }

    [Fact]
    public void Given_parameters_are_laid_over_the_defaults()
    {
        var parameters = StrategySetupFactory.Parameters(Type, Json("""{"scanner":{"topN":2}}""")).Value;

        Json(parameters).GetProperty("scanner").GetProperty("topN").GetInt32().ShouldBe(2);
        Json(parameters).GetProperty("scanner").GetProperty("maPeriod").GetInt32().ShouldBe(200);
        Json(parameters).GetProperty("weakMarketInvestPct").GetDecimal().ShouldBe(0.5m);
    }

    [Theory]
    [InlineData("""{"scanner":{"topN":0}}""")]
    [InlineData("""{"scanner":{"unknown":1}}""")]
    [InlineData("""{"weakMarketInvestPct":2}""")]
    public void Parameters_outside_the_schema_are_refused(string given) =>
        StrategySetupFactory.Parameters(Type, Json(given)).Error.Code.ShouldBe("strategy.parameters_invalid");
}
