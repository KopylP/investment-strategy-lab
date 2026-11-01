namespace InvestmentLab.Rebalancing.Contracts;

public interface IRebalancingRules
{
    IReadOnlyList<RebalancingTradeDto> TradesFor(RebalancingAskDto ask);
}
