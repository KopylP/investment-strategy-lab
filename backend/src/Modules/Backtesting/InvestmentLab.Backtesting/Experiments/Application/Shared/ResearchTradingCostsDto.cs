using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchTradingCostsDto(
    decimal CommissionRate,
    decimal FeePerTrade,
    decimal SpreadBps,
    decimal SlippageBps)
{
    public static ResearchTradingCostsDto From(TradingCosts tradingCosts) => new(
        CommissionRate: tradingCosts.CommissionRate,
        FeePerTrade: tradingCosts.FeePerTrade,
        SpreadBps: tradingCosts.SpreadBps,
        SlippageBps: tradingCosts.SlippageBps);

    public TradingCosts ToDomain() => new(
        CommissionRate: CommissionRate,
        FeePerTrade: FeePerTrade,
        SpreadBps: SpreadBps,
        SlippageBps: SlippageBps);
}
