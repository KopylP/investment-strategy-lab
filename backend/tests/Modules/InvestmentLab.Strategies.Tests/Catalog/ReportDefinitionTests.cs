using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Tests.Catalog;

/// <summary>A strategy type declares its reports; a strategy's parameters may say when each one comes.</summary>
public sealed class ReportDefinitionTests
{
    private static readonly ReportSchedule Monthly = new("monthly", 1, "sunday", "20:00", 0);
    private static readonly ReportDefinition Rebalance = new("rebalance", "Ребаланс", "", "rebalance", Monthly, "reporting.rebalance");

    [Fact]
    public void The_strategys_parameter_sets_the_schedule() =>
        Rebalance.ScheduleFor("""{"reporting":{"rebalance":{"frequency":"weekly","weekday":"friday","time":"18:30"}}}""")
            .ShouldBe(Monthly with { Frequency = "weekly", Weekday = "friday", Time = "18:30" });

    [Theory]
    [InlineData("""{"reporting":{"rebalance":{"frequency":"hourly","minute":75}}}""", "hourly", 0)]   // minute out of range
    [InlineData("""{"reporting":{"rebalance":{"frequency":"yearly","minute":5}}}""", "monthly", 5)]  // not a frequency
    public void An_invalid_field_keeps_the_types_default(string parameters, string frequency, int minute) =>
        Rebalance.ScheduleFor(parameters).ShouldBe(Monthly with { Frequency = frequency, Minute = minute });

    [Theory]
    [InlineData("""{"reporting":{}}""")]                  // not set: the type's default
    [InlineData("""{"reporting":{"rebalance":"weekly"}}""")]
    [InlineData("""{"reporting":"weekly"}""")]
    [InlineData("not json")]
    public void Without_a_schedule_object_the_declared_schedule_holds(string parameters) =>
        Rebalance.ScheduleFor(parameters).ShouldBe(Monthly);

    [Fact]
    public void Without_a_schedule_parameter_or_a_declared_schedule()
    {
        (Rebalance with { ScheduleParameter = null }).ScheduleFor("""{"reporting":{"rebalance":{"frequency":"weekly"}}}""").ShouldBe(Monthly);
        (Rebalance with { Schedule = null }).ScheduleFor("""{"reporting":{}}""").ShouldBe(ReportSchedule.Off);
    }
}
