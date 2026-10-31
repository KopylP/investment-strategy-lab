namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record ReportScheduleDto(bool GenerateOnSchedule, string TimeZone, IReadOnlyList<ScheduledReportDto> Reports);
