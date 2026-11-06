using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Catalog.Infrastructure;

internal sealed class StrategyTypeQueries(IStrategyCatalog catalog) : IStrategyTypeQueries
{
    public async Task<StrategyTypeSchemaDto?> GetTypeAsync(string code, CancellationToken cancellationToken)
    {
        var type = await catalog.FindAsync(code, cancellationToken);

        return type is null ? null : new StrategyTypeSchemaDto(type.Code, type.ParametersSchema, [.. type.Reports.Select(ToDto)]);
    }

    private static StrategyReportDto ToDto(ReportDefinition report)
    {
        var schedule = report.Schedule ?? ReportSchedule.Off;
        var scheduleDto = new StrategyReportScheduleDto(schedule.Frequency, schedule.Day, schedule.Weekday, schedule.Time, schedule.Minute);

        return new StrategyReportDto(report.Code, report.Name, report.Description, report.Mode, scheduleDto, report.SignalKinds);
    }
}
