using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Tests.Definitions;

public sealed class StrategyReportsTests
{
    private static readonly ReportSchedule Monthly = new("monthly", 1, "sunday", "20:00", 0);
    private static readonly ReportSchedule Weekly = new("weekly", 1, "friday", "18:00", 0);

    private static readonly ReportDefinition Rebalance = new("rebalance", "@rebalance.name", "@rebalance.description", "rebalance", Monthly, []);

    [Fact]
    public void Without_own_settings_the_type_declaration_applies()
    {
        var effective = StrategyReports.Of([Rebalance], []).ShouldHaveSingleItem();

        effective.ShouldBe(new EffectiveReport("rebalance", "@rebalance.name", "@rebalance.description", "rebalance", Monthly, [], []), new EffectiveReportComparer());
    }

    [Fact]
    public void Own_settings_replace_schedule_and_signals_but_name_and_mode_stay()
    {
        var signal = Guid.CreateVersion7();
        var own = new VersionReport("rebalance", Weekly, ["volatility-index"], [signal]);

        var effective = StrategyReports.Of([Rebalance], [own]).ShouldHaveSingleItem();

        effective.Schedule.ShouldBe(Weekly);
        effective.SignalKinds.ShouldBe(["volatility-index"]);
        effective.SignalIds.ShouldBe([signal]);
        effective.Name.ShouldBe("@rebalance.name");
        effective.Mode.ShouldBe("rebalance");
    }

    [Fact]
    public void A_report_the_type_does_not_declare_is_an_error() =>
        StrategyReports.Check([Rebalance], [new VersionReport("weekly-digest", Weekly, [], [])])!.Code.ShouldBe("strategy.report_unknown");

    [Fact]
    public void A_report_set_twice_is_an_error()
    {
        var own = new VersionReport("rebalance", Weekly, [], []);

        StrategyReports.Check([Rebalance], [own, own])!.Code.ShouldBe("strategy.report_repeated");
    }

    [Fact]
    public void An_unknown_frequency_is_an_error()
    {
        var own = new VersionReport("rebalance", Weekly with { Frequency = "fortnightly" }, [], []);

        StrategyReports.Check([Rebalance], [own])!.Code.ShouldBe("strategy.report_frequency_unknown");
    }

    [Fact]
    public void Settings_of_declared_reports_pass_the_check() =>
        StrategyReports.Check([Rebalance], [new VersionReport("rebalance", Weekly, [], [])]).ShouldBeNull();

    private sealed class EffectiveReportComparer : IEqualityComparer<EffectiveReport>
    {
        public bool Equals(EffectiveReport? x, EffectiveReport? y) =>
            x is not null && y is not null
            && x.Code == y.Code && x.Name == y.Name && x.Description == y.Description && x.Mode == y.Mode && x.Schedule == y.Schedule
            && x.SignalKinds.SequenceEqual(y.SignalKinds) && x.SignalIds.SequenceEqual(y.SignalIds);

        public int GetHashCode(EffectiveReport report) => report.Code.GetHashCode();
    }
}
