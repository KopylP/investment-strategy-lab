namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record ScheduledReportDto(
    string Code, string Name, string Description, string Mode, ReportTimingDto Schedule, ReportTimingDto StrategySchedule, bool Own, string? Next,
    IReadOnlyList<string> Strategies);
