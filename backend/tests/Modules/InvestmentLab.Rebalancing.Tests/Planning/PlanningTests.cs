using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class ContributionAllocatorTests
{
    private static readonly Guid Eu = Guid.NewGuid(), Us = Guid.NewGuid();
    private static readonly Percentage Band = Percentage.FromPercent(5m);

    [Fact]
    public void Fill_underweight_gives_new_money_to_the_one_that_lags()
    {
        // 60/40 targets, EU 700 + US 200 = 900, +100 → 1000: EU wants 600 (over), US wants 400 (gap 200).
        var allocation = ContributionAllocator.Allocate(100m,
            [new(Eu, Percentage.FromPercent(60m), 700m), new(Us, Percentage.FromPercent(40m), 200m)], ContributionSplit.FillUnderweight, Band);

        allocation.For(Eu).ShouldBe(0m);
        allocation.For(Us).ShouldBe(100m);
        allocation.Transfer.ShouldNotBeNull().FromSubPortfolioId.ShouldBe(Eu);
        allocation.Transfer.Amount.ShouldBe(100m);
    }

    [Fact]
    public void Fixed_split_follows_target_shares_and_adds_up_to_the_cent()
    {
        var allocation = ContributionAllocator.Allocate(100m,
            [new(Eu, Percentage.Of(1m / 3m), 0m), new(Us, Percentage.Of(2m / 3m), 0m)], ContributionSplit.FixedSplit, Band);

        allocation.Shares.Sum(share => share.Amount).ShouldBe(100m);
        allocation.For(Eu).ShouldBe(33.33m);
        allocation.For(Us).ShouldBe(66.67m);
        allocation.Transfer.ShouldBeNull();
    }

    [Fact]
    public void Gaps_are_filled_proportionally_when_both_lag()
    {
        var allocation = ContributionAllocator.Allocate(227m,
            [new(Eu, Percentage.FromPercent(50m), 1000m), new(Us, Percentage.FromPercent(50m), 1000m)], ContributionSplit.FillUnderweight, Band);

        allocation.For(Eu).ShouldBe(113.50m);
        allocation.For(Us).ShouldBe(113.50m);
    }
}

public sealed class GuardrailEnforcerTests
{
    private static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid();

    [Fact]
    public void Targets_above_the_position_cap_are_clipped_and_the_rest_stays_parked()
    {
        var strategy = new StrategyTargets(true, Percentage.Full, [new(A, 0.5m), new(B, 0.5m)], []);

        var (clipped, warnings) = GuardrailEnforcer.Clip(strategy, new GuardrailLimits(Percentage.FromPercent(30m), null), id => id == A ? "A" : "B");

        clipped.Targets.Select(target => target.Weight).ShouldBe([0.3m, 0.3m]);
        warnings.Count.ShouldBe(2);
        warnings[0].ShouldContain("A 50%");
    }

    [Fact]
    public void Targets_above_100_percent_are_scaled_down_no_leverage()
    {
        var strategy = new StrategyTargets(true, Percentage.Full, [new(A, 0.8m), new(B, 0.8m)], []);

        var (clipped, _) = GuardrailEnforcer.Clip(strategy, GuardrailLimits.None, _ => "x");

        clipped.TotalWeight.ShouldBe(1m);
    }
}

public sealed class OrderNettingTests
{
    private static readonly Guid Eu = Guid.NewGuid(), Us = Guid.NewGuid(), Xeon = Guid.NewGuid(), Spy = Guid.NewGuid();

    [Fact]
    public void Opposite_orders_of_one_instrument_become_a_transfer_and_a_net_trade()
    {
        var netted = OrderNetting.Net([
            new(Eu, new PlannedOrder(OrderKind.Unpark, Xeon, 3m, 100m, 300m, "unpark")),
            new(Us, new PlannedOrder(OrderKind.Park, Xeon, 1m, 100m, 100m, "park")),
            new(Us, new PlannedOrder(OrderKind.Buy, Spy, 2m, 500m, 1000m, "buy")),
            new(Eu, new PlannedOrder(OrderKind.Buy, Spy, 1m, 500m, 500m, "buy")),
        ]);

        var transfer = netted.Transfers.ShouldHaveSingleItem();
        (transfer.FromSubPortfolioId, transfer.ToSubPortfolioId, transfer.Quantity).ShouldBe((Eu, Us, 1m));

        netted.BrokerOrders.Count.ShouldBe(2);
        var unpark = netted.BrokerOrders[0];
        (unpark.Kind, unpark.Quantity, unpark.Amount).ShouldBe((OrderKind.Unpark, 2m, 200m));
        var buy = netted.BrokerOrders[1];
        (buy.Kind, buy.Quantity).ShouldBe((OrderKind.Buy, 3m));
        buy.Allocations.Select(allocation => (allocation.SubPortfolioId, allocation.Quantity)).ShouldBe([(Us, 2m), (Eu, 1m)]);
    }
}
