using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public static class EffectiveReportMapping
{
    public static List<StrategyReportDto> ReportsOf(StrategyTypeDescriptor? type, StrategyVersion version)
    {
        var effective = StrategyReports.Of(type?.Reports ?? [], version.Reports);

        return [.. effective.Select(ToDto)];
    }

    private static StrategyReportDto ToDto(EffectiveReport report)
    {
        var schedule = new StrategyReportScheduleDto(report.Schedule.Frequency, report.Schedule.Day, report.Schedule.Weekday, report.Schedule.Time, report.Schedule.Minute);

        return new StrategyReportDto(report.Code, report.Name, report.Description, report.Mode, schedule, report.SignalKinds, report.SignalIds);
    }
}
