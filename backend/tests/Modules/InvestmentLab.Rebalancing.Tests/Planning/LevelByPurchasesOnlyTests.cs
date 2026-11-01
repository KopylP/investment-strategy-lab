using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class LevelByPurchasesOnlyTests
{
    private static readonly Guid Winner = Guid.NewGuid(), Laggard = Guid.NewGuid(), Dropped = Guid.NewGuid();

    [Fact]
    public void Without_the_option_an_overweight_winner_is_trimmed()
    {
        var plan = SubPortfolioRebalancer.Plan(SituationOf(levelByPurchasesOnly: false));

        plan.Orders.ShouldContain(order => order.Kind == OrderKind.Trim && order.InstrumentId == Winner);
    }

    [Fact]
    public void With_the_option_the_contribution_tops_up_the_laggard_and_the_winner_is_not_sold()
    {
        var plan = SubPortfolioRebalancer.Plan(SituationOf(levelByPurchasesOnly: true));

        plan.Orders.ShouldNotContain(order => order.Kind == OrderKind.Trim);
        var purchase = plan.Orders.Single(order => order.Kind == OrderKind.Buy);
        purchase.InstrumentId.ShouldBe(Laggard);
        purchase.Amount.ShouldBe(100m, 0.01m);
    }

    [Fact]
    public void With_the_option_a_fund_the_strategy_dropped_is_still_sold()
    {
        var dropped = new SubPortfolioPosition(Dropped, Quantity.Of(1m), 100m);
        var situation = SituationOf(levelByPurchasesOnly: true);
        situation = situation with { Positions = [.. situation.Positions, dropped] };

        var plan = SubPortfolioRebalancer.Plan(situation);

        plan.Orders.ShouldContain(order => order.Kind == OrderKind.Exit && order.InstrumentId == Dropped);
    }

    private static SubPortfolioSituation SituationOf(bool levelByPurchasesOnly)
    {
        var targets = new[] { new TargetWeight(Winner, 0.5m), new TargetWeight(Laggard, 0.5m) };
        var prices = new Dictionary<Guid, decimal> { [Winner] = 100m, [Laggard] = 100m, [Dropped] = 100m };
        var positions = new[]
        {
            new SubPortfolioPosition(Winner, Quantity.Of(9m), 50m),
            new SubPortfolioPosition(Laggard, Quantity.Of(1m), 100m),
        };

        return new SubPortfolioSituation(
            Strategy: StrategyTargets.Of(true, 1m, targets, []),
            Positions: positions,
            Cash: 0m,
            Contribution: 100m,
            Prices: prices,
            CashInstrumentId: null,
            Policy: RebalancingPolicy.Of(25m, Percentage.FromPercent(10m), true, levelByPurchasesOnly),
            Currency: Currency.EUR,
            Labels: new Dictionary<Guid, string>());
    }
}
