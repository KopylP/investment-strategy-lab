namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public sealed record TrialExecutionCosts(
    decimal CommissionRate,
    decimal FeePerTrade,
    decimal SpreadBps,
    decimal SlippageBps);
