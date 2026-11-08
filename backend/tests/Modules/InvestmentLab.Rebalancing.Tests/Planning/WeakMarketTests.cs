using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class WeakMarketTests
{
    private const decimal ReducedWeight = 0.5m;

    private static readonly Guid Fund = Guid.NewGuid(), Parking = Guid.NewGuid();

    private static readonly Dictionary<Guid, decimal> Prices = new() { [Fund] = 100m, [Parking] = 10m };

    [Fact]
    public void Reduced_holdings_stay_at_their_reduced_target_report_after_report()
    {
        var holding = new Holding(FundShares: 10m, ParkedShares: 0m);

        var first = holding.After(SubPortfolioRebalancer.Plan(ReducedSituation(holding)));
        var second = first.After(SubPortfolioRebalancer.Plan(ReducedSituation(first)));

        first.FundShares.ShouldBe(5m);
        second.ShouldBe(first);
    }

    [Fact]
    public void Money_parked_in_a_weak_market_waits_and_the_report_says_so()
    {
        var holding = new Holding(FundShares: 5m, ParkedShares: 50m);

        var plan = SubPortfolioRebalancer.Plan(ReducedSituation(holding));

        plan.Orders.ShouldBeEmpty();
        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.parked_waits"));
    }

    [Fact]
    public void A_weak_market_that_reduces_holdings_says_how_much_the_targets_cover_without_guessing_slots()
    {
        var holding = new Holding(FundShares: 10m, ParkedShares: 0m);

        var plan = SubPortfolioRebalancer.Plan(ReducedSituation(holding));

        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.partial_targets"));
        plan.Notes.ShouldNotContain(note => note.StartsWith("@report.note.weak_market"));
    }

    [Fact]
    public void A_weak_market_that_invests_part_of_new_money_parks_the_rest_without_selling()
    {
        var targets = new[] { new TargetWeight(Fund, 1m) };
        var situation = ReducedSituation(new Holding(FundShares: 10m, ParkedShares: 0m)) with
        {
            Strategy = StrategyTargets.Of(false, 0.5m, targets, []),
            Contribution = 1000m,
        };

        var plan = SubPortfolioRebalancer.Plan(situation);

        plan.Orders.Single(order => order.Kind == OrderKind.Buy).Amount.ShouldBe(500m);
        plan.Orders.Single(order => order.Kind == OrderKind.Park).Amount.ShouldBe(500m);
        plan.Notes.ShouldContain(note => note.StartsWith("@report.note.weak_market"));
    }

    private static SubPortfolioSituation ReducedSituation(Holding holding)
    {
        var targets = new[] { new TargetWeight(Fund, ReducedWeight) };
        var positions = new[]
        {
            new SubPortfolioPosition(Fund, Quantity.Of(holding.FundShares), 50m),
            new SubPortfolioPosition(Parking, Quantity.Of(holding.ParkedShares), 10m),
        };

        return new SubPortfolioSituation(
            Strategy: StrategyTargets.Of(false, 1m, targets, []),
            Positions: [.. positions.Where(position => position.Quantity.Value > 0m)],
            Cash: 0m,
            Contribution: 0m,
            Prices: Prices,
            CashInstrumentId: Parking,
            Policy: RebalancingPolicy.Of(25m, Percentage.FromPercent(10m), true),
            Currency: Currency.EUR,
            Labels: new Dictionary<Guid, string>());
    }

    private sealed record Holding(decimal FundShares, decimal ParkedShares)
    {
        public Holding After(SubPortfolioPlan plan) => new(
            FundShares: FundShares + ChangeOf(plan, Fund),
            ParkedShares: ParkedShares + ChangeOf(plan, Parking));

        private static decimal ChangeOf(SubPortfolioPlan plan, Guid id) =>
            plan.Orders.Where(order => order.InstrumentId == id).Sum(order => order.Side == OrderSide.Sell ? -order.Quantity : order.Quantity);
    }
}
