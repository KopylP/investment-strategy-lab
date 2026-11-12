using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record TradingCostsDto(
    decimal CommissionRate,
    decimal FeePerTrade,
    decimal SpreadBps,
    decimal SlippageBps)
{
    public static TradingCostsDto From(TradingCosts tradingCosts) => new(
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
