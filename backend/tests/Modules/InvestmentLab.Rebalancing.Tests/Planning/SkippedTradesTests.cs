using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class SkippedTradesTests
{
    private static readonly Guid Leader = Guid.NewGuid(), Laggard = Guid.NewGuid(), Parking = Guid.NewGuid();

    [Fact]
    public void A_trim_reason_states_the_weight_the_fund_has_now()
    {
        var plan = SubPortfolioRebalancer.Plan(SituationOf(leaderCost: 50m));

        var trim = plan.Orders.Single(order => order.Kind == OrderKind.Trim);
        trim.Reason.ShouldStartWith("@report.reason.trim");
        trim.Reason.ShouldContain("\"weight\":0.9");
    }

    [Fact]
    public void An_overweight_fund_at_a_loss_is_kept_and_named_in_a_note()
    {
        var plan = SubPortfolioRebalancer.Plan(SituationOf(leaderCost: 150m));

        plan.Orders.ShouldNotContain(order => order.Kind == OrderKind.Trim);
        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.trims_skipped_at_loss") && note.Contains("LEAD"));
    }

    [Fact]
    public void An_overweight_fund_without_a_known_cost_is_kept_rather_than_sold()
    {
        var plan = SubPortfolioRebalancer.Plan(SituationOf(leaderCost: null));

        plan.Orders.ShouldNotContain(order => order.Kind == OrderKind.Trim);
        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.trims_skipped_at_loss"));
    }

    [Fact]
    public void An_overweight_fund_kept_by_the_account_setting_is_named_in_a_note()
    {
        var situation = SituationOf(leaderCost: 50m) with { Policy = RebalancingPolicy.Of(25m, Percentage.FromPercent(10m), true, true) };

        var plan = SubPortfolioRebalancer.Plan(situation);

        plan.Orders.ShouldNotContain(order => order.Kind == OrderKind.Trim);
        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.trims_skipped_by_setting") && note.Contains("LEAD"));
    }

    [Fact]
    public void A_buy_smaller_than_one_whole_share_is_skipped_and_named_in_a_note()
    {
        var targets = new[] { new TargetWeight(Laggard, 1m) };
        var situation = SituationOf(leaderCost: 50m) with
        {
            Strategy = StrategyTargets.Of(true, 1m, targets, []),
            Positions = [],
            Contribution = 300m,
            Prices = new Dictionary<Guid, decimal> { [Laggard] = 500m, [Parking] = 10m },
            Policy = RebalancingPolicy.Of(25m, Percentage.FromPercent(10m), false),
        };

        var plan = SubPortfolioRebalancer.Plan(situation);

        plan.Orders.ShouldNotContain(order => order.Kind == OrderKind.Buy);
        plan.Orders.Single().Kind.ShouldBe(OrderKind.Park);
        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.buys_below_one_share") && note.Contains("LAG"));
    }

    [Fact]
    public void A_pick_without_a_price_is_named_in_a_note()
    {
        var situation = SituationOf(leaderCost: 50m) with
        {
            Positions = [new SubPortfolioPosition(Leader, Quantity.Of(9m), 50m)],
            Prices = new Dictionary<Guid, decimal> { [Leader] = 100m, [Parking] = 10m },
            Contribution = 500m,
        };

        var plan = SubPortfolioRebalancer.Plan(situation);

        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.no_price") && note.Contains("LAG"));
    }

    private static SubPortfolioSituation SituationOf(decimal? leaderCost)
    {
        var targets = new[] { new TargetWeight(Leader, 0.5m), new TargetWeight(Laggard, 0.5m) };
        var positions = new[]
        {
            new SubPortfolioPosition(Leader, Quantity.Of(9m), leaderCost),
            new SubPortfolioPosition(Laggard, Quantity.Of(1m), 100m),
        };

        return new SubPortfolioSituation(
            Strategy: StrategyTargets.Of(true, 1m, targets, []),
            Positions: positions,
            Cash: 0m,
            Contribution: 0m,
            Prices: new Dictionary<Guid, decimal> { [Leader] = 100m, [Laggard] = 100m, [Parking] = 10m },
            CashInstrumentId: Parking,
            Policy: RebalancingPolicy.Of(25m, Percentage.FromPercent(10m), true),
            Currency: Currency.EUR,
            Labels: new Dictionary<Guid, string> { [Leader] = "LEAD", [Laggard] = "LAG", [Parking] = "PARK" });
    }
}
