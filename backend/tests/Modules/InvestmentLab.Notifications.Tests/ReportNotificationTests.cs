using InvestmentLab.Notifications.Delivery.Application.Integration;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Tests;

public sealed class ReportNotificationTests
{
    private static ReportGeneratedV1 Report(string kind = "Monthly", string? trigger = "Scheduled", int actions = 3) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2026-10", kind, new DateOnly(2026, 10, 23), "PLN", DateTimeOffset.UtcNow,
            [.. Enumerable.Range(0, actions).Select(_ => new PlannedActionDto(Guid.NewGuid(), "Buy", Guid.NewGuid(), 1m, 10m, 10m, [], null))],
            "Revolut", trigger);

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

    [Theory]
    [InlineData(1, "1 дія")]
    [InlineData(5, "5 дій")]
    [InlineData(11, "11 дій")]
    [InlineData(22, "22 дії")]
    public void Actions_are_counted_in_Ukrainian(int actions, string expected) =>
        ReportNotification.For(Report(actions: actions))!.Message.ShouldBe($"«Revolut»: {expected} до виконання.");
}
