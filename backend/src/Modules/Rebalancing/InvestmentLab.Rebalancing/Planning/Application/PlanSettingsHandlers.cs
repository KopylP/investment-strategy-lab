using InvestmentLab.Rebalancing.Planning.Domain;

using System.Globalization;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed record PlanSettingsDto(
    Guid AccountId, decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule)
{
    public static PlanSettingsDto Of(PlanSettings settings) => new(
        settings.AccountId,
        settings.MonthlyContribution,
        settings.MinTrade,
        settings.TrimBand.Value,
        settings.AllowFractionalShares,
        settings.GenerateOnSchedule);
}

public sealed record ChangePlanSettingsCommand(
    decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule = true);

public sealed record ReportTimingDto(string Frequency, int Day, string Weekday, string Time, int Minute)
{
    public static ReportTimingDto Of(Cadence cadence) => new(
        cadence.Frequency.ToString(), cadence.Day, cadence.Weekday.ToString(), cadence.Time.ToString("HH:mm", CultureInfo.InvariantCulture), cadence.Minute);
}

public sealed record ScheduledReportDto(
    string Code, string Name, string Description, string Mode, ReportTimingDto Schedule, ReportTimingDto StrategySchedule, bool Own, string? Next,
    IReadOnlyList<string> Strategies);

public sealed record ReportScheduleDto(bool GenerateOnSchedule, string TimeZone, IReadOnlyList<ScheduledReportDto> Reports);

public sealed record ChangeReportScheduleCommand(string Frequency, int? Day = null, string? Weekday = null, string? Time = null, int? Minute = null);
