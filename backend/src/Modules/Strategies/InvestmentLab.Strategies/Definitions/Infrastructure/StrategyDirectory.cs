using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyDirectory(IStrategyRepository strategies, CatalogSynchronizer catalog) : IStrategyDirectory
{
    public async Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken)
    {
        var types = (await catalog.ListAsync(cancellationToken)).ToDictionary(type => type.Code);

        return [.. (await strategies.ListAsync(cancellationToken)).Select(strategy => new StrategySummaryDto(
            strategy.Id.Value, strategy.Name, strategy.TypeCode, strategy.Latest.Number, strategy.Latest.Universe, strategy.Latest.CashInstrumentId,
            [.. (types.GetValueOrDefault(strategy.TypeCode)?.Reports ?? []).Select(report => new StrategyReportDto(
                report.Code, report.Name, report.Description, report.Mode, ToDto(report.Schedule ?? ReportSchedule.Off)))]))];
    }

    public async Task<IReadOnlyDictionary<string, string>> GetMessages(CancellationToken cancellationToken)
    {
        var types = await catalog.ListFromEngineAsync(cancellationToken);

        return types.SelectMany(type => type.Messages).DistinctBy(message => message.Key).ToDictionary();
    }

    private static StrategyReportScheduleDto ToDto(ReportSchedule schedule) =>
        new(schedule.Frequency, schedule.Day, schedule.Weekday, schedule.Time, schedule.Minute);
}
