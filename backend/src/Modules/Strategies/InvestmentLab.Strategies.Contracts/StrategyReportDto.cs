namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyReportDto(string Code, string Name, string Description, string Mode, StrategyReportScheduleDto Schedule)
{
    public const string Rebalance = "rebalance";
    public const string RiskReview = "riskReview";
}
