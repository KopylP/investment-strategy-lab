namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyReportDto(string Code, string Name, string Description, string Mode, StrategyReportScheduleDto Schedule, IReadOnlyList<string>? SignalKinds = null, IReadOnlyList<Guid>? SignalIds = null)
{
    public const string Rebalance = "rebalance";
    public const string RiskReview = "riskReview";
}
