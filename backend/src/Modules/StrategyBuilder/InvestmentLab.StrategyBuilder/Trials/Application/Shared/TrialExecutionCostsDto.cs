using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed record TrialExecutionCostsDto(
    decimal CommissionRate,
    decimal FeePerTrade,
    decimal SpreadBps,
    decimal SlippageBps)
{
    public static TrialExecutionCostsDto From(TrialExecutionCosts costs) => new(
        CommissionRate: costs.CommissionRate,
        FeePerTrade: costs.FeePerTrade,
        SpreadBps: costs.SpreadBps,
        SlippageBps: costs.SlippageBps);

    public TrialExecutionCosts ToDomain() => new(
        CommissionRate: CommissionRate,
        FeePerTrade: FeePerTrade,
        SpreadBps: SpreadBps,
        SlippageBps: SlippageBps);
}
