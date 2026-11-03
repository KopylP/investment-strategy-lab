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

    private static Strategy Trial() => Strategy.CreateTrial(UserId.New(), "rule-based", Setup(Guid.CreateVersion7()), Now).Value;

    private static Strategy Regular() => Strategy.Create(UserId.New(), "EU", "etf-momentum", Setup(Guid.CreateVersion7()), Now).Value;

    [Fact]
    public void A_trial_strategy_is_named_after_its_id()
    {
        var trial = Trial();

        trial.Name.ShouldBe($"trial-{trial.Id.Value:N}");
        trial.Status.ShouldBe(StrategyStatus.Trial);
        trial.IsTrial.ShouldBeTrue();
    }

    [Fact]
    public void A_trial_strategy_announces_its_first_version()
    {
        var reconfigured = Trial().DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<StrategyReconfigured>();

        reconfigured.Version.ShouldBe(1);
    }

    [Fact]
    public void Only_a_trial_strategy_can_be_deleted() =>
        Regular().Delete(Now).Error.Code.ShouldBe("strategy.not_trial");

    [Fact]
    public void Deleting_a_trial_strategy_announces_it()
    {
        var trial = Trial();
        trial.ClearDomainEvents();

        trial.Delete(Now).IsSuccess.ShouldBeTrue();

        trial.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<StrategyDeleted>().StrategyId.ShouldBe(trial.Id);
    }

    [Fact]
    public void A_regular_strategy_cannot_take_a_trial_version() =>
        Regular().ReconfigureTrial(Setup(Guid.CreateVersion7()), Now).Error.Code.ShouldBe("strategy.not_trial");

    [Fact]
    public void A_trial_strategy_takes_its_next_version()
    {
        var trial = Trial();

        trial.ReconfigureTrial(Setup(Guid.CreateVersion7()), Now.AddMinutes(1)).Value.Number.ShouldBe(2);
    }

    [Fact]
    public void Trial_strategies_are_not_listed()
    {
        var archived = Regular();
        archived.Archive();

        Trial().IsListed.ShouldBeFalse();
        Regular().IsListed.ShouldBeTrue();
        archived.IsListed.ShouldBeTrue();
    }

    [Fact]
    public void Extra_series_may_not_repeat()
    {
        var extra = Guid.CreateVersion7();
        var setup = Setup(Guid.CreateVersion7()) with { ExtraSeries = [extra, extra] };

        Strategy.Create(UserId.New(), "EU", "rule-based", setup, Now).Error.Code.ShouldBe("strategy.extra_series_repeated");
    }
}
