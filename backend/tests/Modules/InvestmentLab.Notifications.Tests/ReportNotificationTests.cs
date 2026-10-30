using InvestmentLab.Notifications.Delivery.Application.Integration;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Tests;

public sealed class ReportNotificationTests
{
    private static readonly DateTimeOffset Generated = new(2026, 10, 23, 18, 0, 0, TimeSpan.Zero);

    /// <param name="thought">How long the strategies thought; null — the event does not say (stored before it did).</param>
    private static ReportGeneratedV1 Report(string kind = "Monthly", string? trigger = "Scheduled", int actions = 3, TimeSpan? thought = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2026-10", kind, new DateOnly(2026, 10, 23), "PLN", Generated,
            [.. Enumerable.Range(0, actions).Select(_ => new PlannedActionDto(Guid.NewGuid(), "Buy", Guid.NewGuid(), 1m, 10m, 10m, [], null))],
            "Revolut", trigger, null, Generated - thought);

    private static ReportRequestFailedV1 Failed(string trigger = "Scheduled", TimeSpan? thought = null, string error = "No strategy answered") =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "rebalance", "Ребаланс", "Monthly", trigger, "2026-11", error,
            Generated - (thought ?? TimeSpan.FromHours(6)), Generated);

    [Fact]
    public void A_scheduled_monthly_report_is_announced_with_its_month_account_and_actions()
    {
        var report = Report();

        var notification = ReportNotification.For(report).ShouldNotBeNull();

        notification.Title.ShouldBe("Рапорт за жовтень 2026 готовий");
        notification.Message.ShouldBe("«Revolut»: 3 дії до виконання.");
        notification.Tag.ShouldBe(ReportNotification.TagOf(report.ReportId));
        notification.Data["actions"].ShouldBe(3);
    }

    [Theory]
    [InlineData("OnDemand", "Monthly", 3)]
    [InlineData(null, "Monthly", 3)]          // stored before the event carried its trigger
    [InlineData("Scheduled", "RiskReview", 0)]
    public void Reports_the_user_asked_for_and_risk_reviews_without_actions_are_not_announced(string? trigger, string kind, int actions) =>
        ReportNotification.For(Report(kind, trigger, actions)).ShouldBeNull();

    [Fact]
    public void A_report_the_user_asked_for_is_announced_when_its_strategies_thought_a_minute_or_more()
    {
        ReportNotification.For(Report(trigger: "OnDemand", thought: TimeSpan.FromSeconds(8))).ShouldBeNull();

        var notification = ReportNotification.For(Report(trigger: "OnDemand", thought: TimeSpan.FromMinutes(40))).ShouldNotBeNull();
        notification.Title.ShouldBe("Рапорт за жовтень 2026 готовий");
    }

    [Fact]
    public void A_report_that_could_not_be_made_is_announced_with_why_and_replaces_the_last_such_notice()
    {
        var failed = Failed(error: "No strategy could evaluate the account: strategy.failed");

        var notification = ReportNotification.For(failed).ShouldNotBeNull();

        notification.Topic.ShouldBe(ReportNotification.FailedTopic);
        notification.Title.ShouldBe("Ребаланс за листопад 2026 не вдалося підготувати");
        notification.Message.ShouldBe("Причина: No strategy could evaluate the account: strategy.failed");
        notification.Tag.ShouldBe(ReportNotification.FailedTagOf(failed.AccountId, "rebalance"));
        ReportNotification.For(Failed(error: new string('x', 1000)))!.Message.Length.ShouldBeLessThan(320);
    }

    [Fact]
    public void A_failure_the_user_saw_happen_is_not_announced() =>
        ReportNotification.For(Failed(trigger: "OnDemand", thought: TimeSpan.FromSeconds(5))).ShouldBeNull();

    [Fact]
    public void Says_when_the_next_report_of_the_kind_comes()
    {
        var next = new NextReport(new DateTime(2026, 11, 1, 20, 0, 0));

        var notification = ReportNotification.For(Report(), next).ShouldNotBeNull();

        notification.Message.ShouldBe("«Revolut»: 3 дії до виконання. Наступний — 01.11.2026 о 20:00.");
        notification.Data["next"].ShouldBe("2026-11-01T20:00");
        ReportNotification.For(Failed(error: "boom"), next)!.Message.ShouldBe("Причина: boom Наступний — 01.11.2026 о 20:00.");
    }

    [Fact]
    public void A_report_that_does_not_come_on_schedule_says_the_next_is_only_on_demand_and_an_old_event_says_nothing()
    {
        ReportNotification.For(Report(), new NextReport(null))!.Message.ShouldBe("«Revolut»: 3 дії до виконання. Наступний — лише вручну.");
        ReportNotification.For(Report(), null)!.Message.ShouldBe("«Revolut»: 3 дії до виконання.");
    }

    [Theory]
    [InlineData(1, "1 дія")]
    [InlineData(5, "5 дій")]
    [InlineData(11, "11 дій")]
    [InlineData(22, "22 дії")]
    public void Actions_are_counted_in_Ukrainian(int actions, string expected) =>
        ReportNotification.For(Report(actions: actions))!.Message.ShouldBe($"«Revolut»: {expected} до виконання.");
}
