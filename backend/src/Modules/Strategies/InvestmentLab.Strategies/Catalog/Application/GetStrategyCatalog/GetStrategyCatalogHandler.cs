using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Catalog.Application.GetStrategyCatalog;

public sealed class GetStrategyCatalogHandler(IStrategyCatalog catalog)
{
    public async Task<IReadOnlyList<StrategyTypeDto>> Handle(CancellationToken cancellationToken)
    {
        var types = await catalog.ListAsync(cancellationToken);

        return [.. types.Select(ToDto)];
    }

    private static StrategyTypeDto ToDto(StrategyTypeDescriptor type)
    {
        var parametersSchema = JsonDocument.Parse(type.ParametersSchema).RootElement;
        var defaultParameters = type.DefaultParameters is null ? (JsonElement?)null : JsonDocument.Parse(type.DefaultParameters).RootElement;
        var reports = type.Reports.Select(ToDto).ToList();

        return new StrategyTypeDto(
            Code: type.Code,
            Name: type.Name,
            Description: type.Description,
            Engine: type.Engine,
            EngineVersion: type.EngineVersion,
            SupportsState: type.SupportsState,
            RequiredSeriesRoles: type.RequiredSeriesRoles,
            ParametersSchema: parametersSchema,
            DefaultParameters: defaultParameters,
            Reports: reports);
    }

    private static ReportDefinitionDto ToDto(ReportDefinition report) => new(
        Code: report.Code,
        Name: report.Name,
        Description: report.Description,
        Mode: report.Mode,
        Schedule: report.Schedule is { } schedule ? ToDto(schedule) : null,
        SignalKinds: report.SignalKinds);

    private static ReportScheduleDto ToDto(ReportSchedule schedule) =>
        new(schedule.Frequency, schedule.Day, schedule.Weekday, schedule.Time, schedule.Minute);
}
