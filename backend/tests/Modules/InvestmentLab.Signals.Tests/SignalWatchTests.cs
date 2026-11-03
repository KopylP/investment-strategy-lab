using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Tests;

public sealed class SignalWatchTests
{
    private static readonly DateOnly FirstDay = new(2008, 1, 2);

    [Fact]
    public void A_level_signal_fires_once_when_crossed_and_again_only_after_it_rearms()
    {
        var watch = new SignalWatch(BuiltInSignalRules.ConditionOf(BuiltInSignalKind.VolatilityIndex));
        decimal[] levels = [20m, 31m, 35m, 27m, 24m, 32m];

        var fired = FiringsOf(watch, levels, "vix");

        fired.ShouldBe([false, true, false, false, false, true]);
    }

    [Fact]
    public void An_account_fall_is_measured_from_its_highest_value()
    {
        var watch = new SignalWatch(BuiltInSignalRules.ConditionOf(BuiltInSignalKind.AccountDrawdown));
        decimal[] values = [1.0m, 1.5m, 1.2m, 1.04m, 1.06m];

        var fired = FiringsOf(watch, values, "account");

        fired.ShouldBe([false, false, false, true, false]);
    }

    [Fact]
    public void A_position_fall_counts_only_since_the_position_was_bought()
    {
        var watch = new SignalWatch(BuiltInSignalRules.ConditionOf(BuiltInSignalKind.PositionDrawdown));
        var boughtOn = FirstDay.AddDays(2);

        watch.FiresOn(FirstDay, [new WatchedReading("fund", 200m)]);
        watch.FiresOn(FirstDay.AddDays(1), [new WatchedReading("fund", 120m)]);
        var firedAtPurchase = watch.FiresOn(boughtOn, [new WatchedReading("fund", 100m, boughtOn)]);
        var firedAfterFall = watch.FiresOn(boughtOn.AddDays(1), [new WatchedReading("fund", 79m, boughtOn)]);

        firedAtPurchase.ShouldBeFalse();
        firedAfterFall.ShouldBeTrue();
    }

    [Fact]
    public void A_position_sold_while_alarmed_can_fire_again_when_held_once_more()
    {
        var watch = new SignalWatch(BuiltInSignalRules.ConditionOf(BuiltInSignalKind.PositionDrawdown));

        watch.FiresOn(FirstDay, [new WatchedReading("fund", 100m, FirstDay)]);
        watch.FiresOn(FirstDay.AddDays(1), [new WatchedReading("fund", 70m, FirstDay)]);
        watch.FiresOn(FirstDay.AddDays(2), []);
        var boughtAgain = FirstDay.AddDays(3);
        watch.FiresOn(boughtAgain, [new WatchedReading("fund", 70m, boughtAgain)]);
        var fired = watch.FiresOn(boughtAgain.AddDays(1), [new WatchedReading("fund", 50m, boughtAgain)]);

        fired.ShouldBeTrue();
    }

    [Fact]
    public void An_unknown_built_in_signal_kind_fails_loudly()
    {
        Should.Throw<ArgumentException>(() => BuiltInSignalRules.ConditionOf("market-crash"));
    }

    private static List<bool> FiringsOf(SignalWatch watch, decimal[] values, string subject) =>
        [.. values.Select((value, index) => watch.FiresOn(FirstDay.AddDays(index), [new WatchedReading(subject, value)]))];
    [Fact]
    public void A_day_fall_is_replayed_from_consecutive_closes()
    {
        var condition = SignalCondition.TryOf(SignalMetric.DayFall, SignalDirection.Above, 0.05m, null, null).Value;
        var watch = new SignalWatch(condition);

        watch.FiresOn(FirstDay, [new WatchedReading("fund", 100m)]).ShouldBeFalse();
        watch.FiresOn(FirstDay.AddDays(1), [new WatchedReading("fund", 94m)]).ShouldBeTrue();
    }

    [Fact]
    public void A_loss_from_cost_is_replayed_with_the_unit_cost()
    {
        var condition = SignalCondition.TryOf(SignalMetric.LossFromCost, SignalDirection.Above, 0.12m, null, null).Value;
        var watch = new SignalWatch(condition);

        watch.FiresOn(FirstDay, [new WatchedReading("fund", 85m, UnitCost: 100m)]).ShouldBeTrue();
    }

    [Fact]
    public void A_loss_without_a_unit_cost_is_not_measured()
    {
        var condition = SignalCondition.TryOf(SignalMetric.LossFromCost, SignalDirection.Above, 0.12m, null, null).Value;
        var watch = new SignalWatch(condition);

        watch.FiresOn(FirstDay, [new WatchedReading("fund", 85m)]).ShouldBeFalse();
        watch.Alarmed.ShouldBeEmpty();
    }

    [Fact]
    public void Alarmed_subjects_stay_until_they_rearm()
    {
        var watch = new SignalWatch(BuiltInSignalRules.ConditionOf(BuiltInSignalKind.VolatilityIndex));

        watch.FiresOn(FirstDay, [new WatchedReading("index", 31m)]).ShouldBeTrue();
        watch.FiresOn(FirstDay.AddDays(1), [new WatchedReading("index", 28m)]).ShouldBeFalse();
        watch.Alarmed.ShouldBe(["index"]);
        watch.FiresOn(FirstDay.AddDays(2), [new WatchedReading("index", 20m)]).ShouldBeFalse();
        watch.Alarmed.ShouldBeEmpty();
    }

}
