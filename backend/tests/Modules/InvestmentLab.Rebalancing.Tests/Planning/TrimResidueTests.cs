using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class TrimResidueTests
{
    private static readonly Guid Trimmed = Guid.NewGuid(), Parking = Guid.NewGuid();

    public static TheoryData<decimal> TargetWeights =>
    [
        0.07360446106604283m,
        0.12345678901234568m,
        0.09876543210987654m,
        0.33333333333333331m,
        0.18849555921538758m,
        0.05000000000000000277m,
        0.0736m,
        0.0737m,
        0.1885m,
        0.12m,
        0.07m,
    ];

    [Theory]
    [MemberData(nameof(TargetWeights))]
    public void A_fund_trimmed_to_its_target_leaves_no_purchase_behind(decimal targetWeight)
    {
        var situation = SituationOf(targetWeight);

        var plan = SubPortfolioRebalancer.Plan(situation);

        plan.Orders.ShouldContain(order => order.Kind == OrderKind.Trim && order.InstrumentId == Trimmed);
        plan.Orders.ShouldNotContain(order => order.Kind == OrderKind.Buy);
    }

    private static SubPortfolioSituation SituationOf(decimal targetWeight)
    {
        var targets = new[] { new TargetWeight(Trimmed, targetWeight) };
        var prices = new Dictionary<Guid, decimal> { [Trimmed] = 137.4185390625m, [Parking] = 141.2276m };
        var positions = new[] { new SubPortfolioPosition(Trimmed, Quantity.Of(218.73196078125012345m), 80m) };

        return new SubPortfolioSituation(
            Strategy: StrategyTargets.Of(false, 1m, targets, []),
            Positions: positions,
            Cash: 0m,
            Contribution: 0m,
            Prices: prices,
            CashInstrumentId: Parking,
            Policy: RebalancingPolicy.Of(25m, Percentage.FromPercent(10m), true, false),
            Currency: Currency.EUR,
            Labels: new Dictionary<Guid, string>());
    }
}
