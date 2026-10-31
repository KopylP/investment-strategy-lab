using InvestmentLab.SharedKernel;
using InvestmentLab.Signals.Monitoring.Application.Integration;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Tests;

public sealed class SignalEventMappersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 22, 0, 0, TimeSpan.Zero);
    private static readonly UserId Owner = UserId.Of(Guid.NewGuid());

    private static SignalFired Fired(SignalCondition condition, params SubjectReading[] subjects) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Owner, null, "Сигнал", condition, subjects, Now);

    [Fact]
    public void A_fall_reads_as_a_negative_percent_and_carries_the_newest_data_date()
    {
        var condition = SignalCondition.TryOf(SignalMetric.DrawdownFromPeak, SignalDirection.Above, 0.15m, null, null).Value;
        var older = new SubjectReading("a", "SXR8", 0.162m, new DateOnly(2026, 10, 29));
        var newer = new SubjectReading("b", "EQQQ", 0.2m, new DateOnly(2026, 10, 30));

        var triggered = new SignalFiredMapper().Map(Fired(condition, older, newer));

        triggered.Subjects[0].Text.ShouldBe("SXR8: −16,2%");
        triggered.DataDate.ShouldBe(new DateOnly(2026, 10, 30));
        triggered.OwnerId.ShouldBe(Owner.Value);
    }

    [Fact]
    public void A_level_reads_as_it_is()
    {
        var condition = SignalCondition.TryOf(SignalMetric.Level, SignalDirection.Above, 30m, null, null).Value;
        var vix = new SubjectReading("v", "^VIX", 31.4m, new DateOnly(2026, 10, 30));

        var triggered = new SignalFiredMapper().Map(Fired(condition, vix));

        triggered.Subjects[0].Text.ShouldBe("^VIX: 31,4");
    }

    [Fact]
    public void A_cleared_event_says_why()
    {
        var cleared = new SignalCleared(Guid.NewGuid(), Owner, "Сигнал", ClearReason.PositionClosed, ["SXR8"], 1, Now);

        var integrationEvent = new SignalClearedMapper().Map(cleared);

        integrationEvent.Reason.ShouldBe("PositionClosed");
        integrationEvent.StillActive.ShouldBe(1);
    }
}
