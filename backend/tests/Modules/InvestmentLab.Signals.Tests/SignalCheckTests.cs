using InvestmentLab.SharedKernel;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Tests;

public sealed class SignalCheckTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 22, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly DataDate = new(2026, 10, 30);

    private static Signal DrawdownOf30Percent()
    {
        var condition = SignalCondition.TryOf(SignalMetric.DrawdownFromPeak, SignalDirection.Above, 0.30m, null, null).Value;

        return Signal.Create(UserId.Of(Guid.NewGuid()), "Просадка", SignalTarget.Account(Guid.NewGuid()), condition, Cadence.Off).Value;
    }

    private static SubjectReadings Readings(params (string Key, decimal Value)[] values)
    {
        var measured = values.Select(each => new SubjectReading(each.Key, each.Key.ToUpperInvariant(), each.Value, DataDate)).ToList();
        var live = values.Select(each => each.Key).ToHashSet();

        return new SubjectReadings(measured, live);
    }

    private static SignalOccurrence FiredOccurrence(Signal signal)
    {
        var outcome = signal.Check([], Readings(("a", 0.31m)), Now);

        signal.ClearDomainEvents();

        return outcome.Fired[0].Occurrence;
    }

    private static SignalSettings SettingsOf(Signal signal, SignalCondition? condition = null, Cadence? schedule = null, bool enabled = true) =>
        new(signal.Name, condition ?? signal.Condition, schedule ?? signal.Schedule, enabled);

    [Fact]
    public void A_fall_past_the_threshold_fires_once_and_stays_silent_while_it_lasts()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        var second = signal.Check([occurrence], Readings(("a", 0.40m)), Now);

        second.TriggerId.ShouldBeNull();
        second.Fired.ShouldBeEmpty();
        second.StillActive.ShouldBe(1);
        signal.DomainEvents.ShouldBeEmpty();
        occurrence.IsOpen.ShouldBeTrue();
        occurrence.LatestValue.ShouldBe(0.40m);
    }

    [Fact]
    public void A_value_hovering_above_the_rearm_level_stays_the_same_alarm()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        var dipped = signal.Check([occurrence], Readings(("a", 0.28m)), Now);
        var back = signal.Check([occurrence], Readings(("a", 0.31m)), Now);

        dipped.Recovered.ShouldBeEmpty();
        back.Fired.ShouldBeEmpty();
        occurrence.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void It_recovers_at_the_rearm_level_and_may_then_fire_again()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        var recovered = signal.Check([occurrence], Readings(("a", 0.25m)), Now);

        recovered.Recovered.ShouldHaveSingleItem();
        recovered.StillActive.ShouldBe(0);
        occurrence.ClearReason.ShouldBe(ClearReason.Recovered);

        signal.Check([], Readings(("a", 0.30m)), Now).Fired.ShouldHaveSingleItem();
    }

    [Fact]
    public void Subjects_crossing_together_share_one_firing_and_one_event()
    {
        var signal = DrawdownOf30Percent();

        var outcome = signal.Check([], Readings(("a", 0.31m), ("b", 0.35m), ("c", 0.10m)), Now);

        outcome.Fired.Count.ShouldBe(2);
        outcome.Fired.ShouldAllBe(alarm => alarm.Occurrence.TriggerId == outcome.TriggerId);

        var fired = signal.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SignalFired>();
        fired.TriggerId.ShouldBe(outcome.TriggerId!.Value);
        fired.Subjects.Select(subject => subject.Key).ShouldBe(["a", "b"]);
    }

    [Fact]
    public void Recovering_raises_a_cleared_event_saying_what_is_left()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        signal.Check([occurrence], Readings(("a", 0.10m)), Now);

        var cleared = signal.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SignalCleared>();
        cleared.Reason.ShouldBe(ClearReason.Recovered);
        cleared.Subjects.ShouldBe(["A"]);
        cleared.StillActive.ShouldBe(0);
    }

    [Fact]
    public void A_sold_position_closes_without_saying_it_recovered()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        var outcome = signal.Check([occurrence], SubjectReadings.Nothing, Now);

        outcome.Gone.ShouldHaveSingleItem();
        outcome.Recovered.ShouldBeEmpty();
        occurrence.ClearReason.ShouldBe(ClearReason.PositionClosed);

        var cleared = signal.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SignalCleared>();
        cleared.Reason.ShouldBe(ClearReason.PositionClosed);
    }

    [Fact]
    public void A_held_position_without_a_usable_quote_keeps_its_alarm()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        var outcome = signal.Check([occurrence], SubjectReadings.Unmeasured("a"), Now);

        outcome.Gone.ShouldBeEmpty();
        outcome.StillActive.ShouldBe(1);
        occurrence.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void A_new_condition_closes_what_was_in_alarm_by_the_old_one()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);
        var stricter = SignalCondition.TryOf(SignalMetric.DrawdownFromPeak, SignalDirection.Above, 0.40m, null, null).Value;

        signal.Change(SettingsOf(signal, stricter), [occurrence], Now).IsSuccess.ShouldBeTrue();

        occurrence.ClearReason.ShouldBe(ClearReason.Reconfigured);
        signal.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SignalCleared>().StillActive.ShouldBe(0);
    }

    [Fact]
    public void Switching_off_closes_what_was_in_alarm()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        signal.Change(SettingsOf(signal, enabled: false), [occurrence], Now);

        occurrence.ClearReason.ShouldBe(ClearReason.Disabled);
    }

    [Fact]
    public void Renaming_leaves_the_alarm_open()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        signal.Change(SettingsOf(signal) with { Name = "Інша назва" }, [occurrence], Now);

        occurrence.IsOpen.ShouldBeTrue();
        signal.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Deleting_closes_the_alarms_and_raises_deleted()
    {
        var signal = DrawdownOf30Percent();
        var occurrence = FiredOccurrence(signal);

        signal.Delete([occurrence], Now).IsSuccess.ShouldBeTrue();

        occurrence.ClearReason.ShouldBe(ClearReason.Deleted);
        signal.DomainEvents.Select(domainEvent => domainEvent.GetType()).ShouldBe([typeof(SignalCleared), typeof(SignalDeleted)]);
    }

    [Fact]
    public void A_built_in_signal_cannot_be_deleted()
    {
        var condition = SignalCondition.TryOf(SignalMetric.Level, SignalDirection.Above, 30m, null, null).Value;
        var target = SignalTarget.Instrument(Guid.NewGuid());
        var signal = Signal.Create(UserId.Of(Guid.NewGuid()), "VIX", target, condition, Cadence.Off, "vix:key").Value;

        signal.Delete([], Now).Error.Code.ShouldBe("signal.built_in");
        signal.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(SignalDirection.Above, 30, 31, true)]
    [InlineData(SignalDirection.Above, 30, 29, false)]
    [InlineData(SignalDirection.Below, 12, 11, true)]
    [InlineData(SignalDirection.Below, 12, 13, false)]
    public void A_level_is_crossed_in_its_own_direction(SignalDirection direction, int threshold, int value, bool crossed)
    {
        var condition = SignalCondition.TryOf(SignalMetric.Level, direction, threshold, null, null).Value;

        condition.IsCrossedBy(value).ShouldBe(crossed);
    }

    [Fact]
    public void The_default_rearm_level_is_a_sixth_of_the_way_back()
    {
        var condition = SignalCondition.TryOf(SignalMetric.DrawdownFromPeak, SignalDirection.Above, 0.30m, null, null).Value;

        condition.RearmAt.ShouldBe(0.25m);
    }

    [Fact]
    public void A_condition_with_impossible_numbers_is_refused()
    {
        var rearmPastThreshold = SignalCondition.TryOf(SignalMetric.DrawdownFromPeak, SignalDirection.Above, 0.30m, 0.35m, null);
        var fallOverHundredPercent = SignalCondition.TryOf(SignalMetric.DayFall, SignalDirection.Above, 1.5m, null, null);

        rearmPastThreshold.Error.Code.ShouldBe("signal.rearm");
        fallOverHundredPercent.Error.Code.ShouldBe("signal.threshold");
    }

    [Fact]
    public void A_metric_must_make_sense_for_the_target()
    {
        var loss = SignalCondition.TryOf(SignalMetric.LossFromCost, SignalDirection.Above, 0.1m, null, null).Value;
        var owner = UserId.Of(Guid.NewGuid());

        var forInstrument = Signal.Create(owner, "VIX", SignalTarget.Instrument(Guid.NewGuid()), loss, Cadence.Off);
        var forPositions = Signal.Create(owner, "Позиції", SignalTarget.EachPosition(Guid.NewGuid()), loss, Cadence.Off);

        forInstrument.Error.Code.ShouldBe("signal.metric");
        forPositions.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_signal_after_new_data_looks_every_ten_minutes()
    {
        var signal = DrawdownOf30Percent();
        signal.IsDue(Now, ToLocal).ShouldBeTrue();

        signal.Check([], SubjectReadings.Nothing, Now);

        signal.IsDue(Now.AddMinutes(9), ToLocal).ShouldBeFalse();
        signal.IsDue(Now.AddMinutes(10), ToLocal).ShouldBeTrue();
    }

    [Fact]
    public void A_signal_on_a_schedule_comes_at_its_time()
    {
        var signal = DrawdownOf30Percent();
        var daily = Cadence.Of("daily", 1, "", "23:30", 0);
        signal.Change(SettingsOf(signal, schedule: daily), [], Now);

        signal.Check([], SubjectReadings.Nothing, Now);

        signal.IsDue(Now.AddHours(1), ToLocal).ShouldBeFalse();
        signal.IsDue(Now.AddHours(2), ToLocal).ShouldBeTrue();
    }

    [Fact]
    public void A_disabled_signal_is_never_due()
    {
        var signal = DrawdownOf30Percent();

        signal.Change(SettingsOf(signal, enabled: false), [], Now);

        signal.IsDue(Now.AddDays(1), ToLocal).ShouldBeFalse();
    }

    [Fact]
    public void Measures_read_the_last_value_against_the_peak_and_the_day_before()
    {
        SignalMetrics.DrawdownFromPeak([100m, 120m, 90m]).ShouldBe(0.25m);
        SignalMetrics.DrawdownFromPeak([]).ShouldBeNull();
        SignalMetrics.DayFall([100m, 96.5m]).ShouldBe(0.035m);
        SignalMetrics.DayFall([100m]).ShouldBeNull();
        SignalMetrics.LossFromCost(90m, 100m).ShouldBe(0.1m);
        SignalMetrics.LossFromCost(110m, 100m).ShouldBe(-0.1m);
    }

    private static DateTime ToLocal(DateTimeOffset instant) => instant.UtcDateTime;
}
