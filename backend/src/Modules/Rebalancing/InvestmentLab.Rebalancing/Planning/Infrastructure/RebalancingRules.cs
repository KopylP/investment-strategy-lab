using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class RebalancingRules : IRebalancingRules
{
    public IReadOnlyList<RebalancingTradeDto> TradesFor(RebalancingAskDto ask)
    {
        var plan = SubPortfolioRebalancer.Plan(SituationOf(ask));

        return [.. plan.Orders.Select(TradeOf)];
    }

    private static SubPortfolioSituation SituationOf(RebalancingAskDto ask)
    {
        var targets = ask.Targets.Select(target => new TargetWeight(target.InstrumentId, target.Weight)).ToList();
        var positions = ask.Positions.Select(position => new SubPortfolioPosition(position.InstrumentId, Quantity.Of(position.Quantity), position.AverageCost)).ToList();

        return new SubPortfolioSituation(
            Strategy: StrategyTargets.Of(ask.MarketIsFavorable, ask.InvestFraction, targets, []),
            Positions: positions,
            Cash: ask.Cash,
            Contribution: 0m,
            Prices: ask.Prices,
            CashInstrumentId: ask.CashInstrumentId,
            Policy: RebalancingPolicy.Default,
            Currency: Currency.Of(ask.Currency),
            Labels: new Dictionary<Guid, string>());
    }

    private static RebalancingTradeDto TradeOf(PlannedOrder order) =>
        new(order.InstrumentId, order.Side == OrderSide.Sell, order.Quantity, order.Amount);
}
